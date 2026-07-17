"""Async TimescaleDB writer (CLAUDE.md §2, §6).

The engine ONLY writes here — the API/UI only read. The hot path uses a
fire-and-forget queue so a slow DB never stalls the tick/strategy loop.
Batch collectors can call `insert_option_ticks` directly.
"""
from __future__ import annotations

import asyncio
import logging

import asyncpg

from engine.config import Config
from engine.models.market import Tick

logger = logging.getLogger(__name__)

# Upsert so re-running a collection in the same second updates rather than errors on PK.
_INSERT_OPTION_TICK = """
INSERT INTO option_ticks ("Time","Symbol","Strike","OptionType","Ltp","Volume","Oi","Iv")
VALUES ($1,$2,$3,$4,$5,$6,$7,$8)
ON CONFLICT ("Time","Symbol","Strike","OptionType")
DO UPDATE SET "Ltp"=EXCLUDED."Ltp","Volume"=EXCLUDED."Volume",
              "Oi"=EXCLUDED."Oi","Iv"=EXCLUDED."Iv";
"""


class TimescaleWriter:
    def __init__(self, cfg: Config, max_queue: int = 10_000) -> None:
        self._dsn = cfg.postgres_dsn
        self._pool: asyncpg.Pool | None = None
        self._queue: asyncio.Queue[Tick] = asyncio.Queue(maxsize=max_queue)
        self._task: asyncio.Task | None = None

    async def start(self) -> None:
        self._pool = await asyncpg.create_pool(self._dsn, min_size=1, max_size=5)
        self._task = asyncio.create_task(self._drain())
        logger.info("TimescaleWriter connected")

    @property
    def pool(self) -> asyncpg.Pool:
        """Shared connection pool (used by the session-report aggregator)."""
        assert self._pool is not None, "writer not started"
        return self._pool

    # --- Hot path: fire-and-forget ---
    def write_tick(self, tick: Tick) -> None:
        try:
            self._queue.put_nowait(tick)
        except asyncio.QueueFull:
            logger.warning("timescale queue full — dropping tick to protect hot path")

    # --- Batch path: used by the collector ---
    async def insert_option_ticks(self, ticks: list[Tick]) -> int:
        if not self._pool:
            raise RuntimeError("writer not started")
        rows = [
            (t.time, t.symbol, t.strike, t.option_type.value,
             round(t.ltp, 2), int(t.volume), int(t.oi), round(t.iv, 4))
            for t in ticks
        ]
        async with self._pool.acquire() as conn:
            await conn.executemany(_INSERT_OPTION_TICK, rows)
        return len(rows)

    # --- OHLC candles (hypertable) ---
    async def insert_candles(self, candles: list) -> int:
        assert self._pool is not None
        rows = [(c.time, c.symbol, c.interval, round(c.open, 2), round(c.high, 2),
                 round(c.low, 2), round(c.close, 2), int(c.volume)) for c in candles]
        async with self._pool.acquire() as conn:
            await conn.executemany(
                'INSERT INTO ohlc_candles ("Time","Symbol","Interval","Open","High","Low","Close","Volume")'
                ' VALUES ($1,$2,$3,$4,$5,$6,$7,$8)'
                ' ON CONFLICT ("Time","Symbol","Interval") DO UPDATE SET'
                ' "Open"=EXCLUDED."Open","High"=EXCLUDED."High","Low"=EXCLUDED."Low",'
                ' "Close"=EXCLUDED."Close","Volume"=EXCLUDED."Volume";', rows)
        return len(rows)

    # --- Engine run tracking (one row per application run) ---
    async def start_engine_session(self, session_date, started_at, mode: str) -> int:
        """Insert a run row at startup (EndedAt null). Returns the row id."""
        assert self._pool is not None
        return await self._pool.fetchval(
            'INSERT INTO engine_sessions ("SessionDate","StartedAt","Mode","TotalTrades","GrossPnl")'
            ' VALUES ($1,$2,$3,0,0) RETURNING "Id";',
            session_date, started_at, mode,
        )

    async def end_engine_session(self, session_id: int, ended_at, duration_sec: float,
                                 stop_reason: str, total_trades: int, gross_pnl: float) -> None:
        """Finalize the run row at shutdown with end time, duration and stats."""
        assert self._pool is not None
        await self._pool.execute(
            'UPDATE engine_sessions SET "EndedAt"=$2,"DurationSeconds"=$3,"StopReason"=$4,'
            '"TotalTrades"=$5,"GrossPnl"=$6 WHERE "Id"=$1;',
            session_id, ended_at, round(duration_sec, 1), stop_reason,
            total_trades, round(gross_pnl, 2),
        )

    # --- OI snapshots (hypertable, per minute) ---
    async def insert_oi_snapshots(self, time, symbol: str,
                                  rows: list[tuple[int, str, int, int]]) -> int:
        """rows = [(strike, 'CE'|'PE', oi, oi_change)]."""
        assert self._pool is not None
        data = [(time, symbol, r[0], r[1], r[2], r[3]) for r in rows]
        async with self._pool.acquire() as conn:
            await conn.executemany(
                'INSERT INTO oi_snapshots ("Time","Symbol","Strike","OptionType","Oi","OiChange")'
                ' VALUES ($1,$2,$3,$4,$5,$6)'
                ' ON CONFLICT ("Time","Symbol","Strike","OptionType") DO UPDATE SET'
                ' "Oi"=EXCLUDED."Oi","OiChange"=EXCLUDED."OiChange";', data)
        return len(data)

    # --- Market context (hypertable, per minute) ---
    async def insert_market_context(self, time, symbol: str, regime: str,
                                    volume_vs_avg: float, vix: float, dte: int,
                                    htf_trend: str | None, premarket_bias: str | None) -> None:
        assert self._pool is not None
        await self._pool.execute(
            'INSERT INTO market_context ("Time","Symbol","Regime","VolumeVsAvg","Vix","Dte","HtfTrend","PremarketBias")'
            ' VALUES ($1,$2,$3,$4,$5,$6,$7,$8)'
            ' ON CONFLICT ("Time","Symbol") DO UPDATE SET'
            ' "Regime"=EXCLUDED."Regime","VolumeVsAvg"=EXCLUDED."VolumeVsAvg","Vix"=EXCLUDED."Vix";',
            time, symbol, regime, round(volume_vs_avg, 3), round(vix, 2), dte,
            htf_trend, premarket_bias)

    # --- Decision + trade persistence (normal tables) ---
    async def insert_signal(self, time, signal_type: str, confidence: float,
                            features_json: str, outcome: str | None = None) -> int:
        assert self._pool is not None
        return await self._pool.fetchval(
            'INSERT INTO strategy_signals ("Time","SignalType","Confidence","FeaturesJson","Outcome")'
            ' VALUES ($1,$2,$3,$4::jsonb,$5) RETURNING "Id";',
            time, signal_type, confidence, features_json, outcome,
        )

    async def insert_no_trade(self, time, reason: str, context_json: str) -> None:
        assert self._pool is not None
        await self._pool.execute(
            'INSERT INTO no_trades ("Time","Reason","ContextJson") VALUES ($1,$2,$3::jsonb);',
            time, reason, context_json,
        )

    async def open_trade(self, symbol: str, side: str, entry_time, entry_price: float,
                         qty: int, signal_id: int | None, setup_name: str,
                         htf_trend: str | None, ltf_trigger: str | None) -> int:
        assert self._pool is not None
        return await self._pool.fetchval(
            'INSERT INTO paper_trades ("Symbol","Side","EntryTime","EntryPrice","Qty",'
            '"SignalId","SetupName","HtfTrend","LtfTrigger")'
            ' VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9) RETURNING "Id";',
            symbol, side, entry_time, round(entry_price, 2), qty,
            signal_id, setup_name, htf_trend, ltf_trigger,
        )

    async def close_trade(self, trade_id: int, exit_time, exit_price: float,
                          pnl: float, hold_seconds: float) -> None:
        assert self._pool is not None
        await self._pool.execute(
            'UPDATE paper_trades SET "ExitTime"=$2,"ExitPrice"=$3,"Pnl"=$4,'
            '"HoldDuration"=make_interval(secs => $5) WHERE "Id"=$1;',
            trade_id, exit_time, round(exit_price, 2), round(pnl, 2), hold_seconds,
        )

    async def set_signal_outcome(self, signal_id: int, outcome: str) -> None:
        assert self._pool is not None
        await self._pool.execute(
            'UPDATE strategy_signals SET "Outcome"=$2 WHERE "Id"=$1;', signal_id, outcome,
        )

    async def _drain(self) -> None:
        assert self._pool is not None
        while True:
            tick = await self._queue.get()
            try:
                await self.insert_option_ticks([tick])
            except Exception:
                logger.exception("failed writing tick")
            finally:
                self._queue.task_done()

    async def stop(self) -> None:
        if self._task:
            self._task.cancel()
        if self._pool:
            await self._pool.close()
