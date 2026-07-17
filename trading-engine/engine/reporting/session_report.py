"""End-of-session report -> session_reports table (CLAUDE.md §5c).

Aggregates the day's paper_trades + no_trades into one summary row:
trades, wins/losses, no-trades, gross P&L, max drawdown, win rate, avg R:R,
rolling accuracy, best setup. Idempotent per date (upsert on SessionDate), so
it can be regenerated on every shutdown / restart without duplicating.

Called from run_paper.py at square-off / Ctrl+C. All time filtering is by IST
trading date.
"""
from __future__ import annotations

from dataclasses import dataclass
from datetime import date

import asyncpg

IST_TZ = "Asia/Kolkata"


@dataclass
class SessionReport:
    session_date: date
    total_trades: int
    wins: int
    losses: int
    no_trades: int
    gross_pnl: float
    max_drawdown: float
    win_rate: float
    avg_rr: float
    rolling_accuracy: float
    best_setup: str = ""
    notes: str = ""


async def build_and_store(pool: asyncpg.Pool, session_date: date) -> SessionReport:
    """Aggregate the day's closed trades + no-trades and upsert one report row."""
    # Closed trades entered on this IST date, ordered for drawdown calc.
    trades = await pool.fetch(
        'SELECT "Pnl", "SetupName" FROM paper_trades'
        ' WHERE ("EntryTime" AT TIME ZONE $2)::date = $1 AND "Pnl" IS NOT NULL'
        ' ORDER BY COALESCE("ExitTime", "EntryTime");',
        session_date, IST_TZ,
    )
    no_trades = await pool.fetchval(
        'SELECT count(*) FROM no_trades WHERE ("Time" AT TIME ZONE $2)::date = $1;',
        session_date, IST_TZ,
    )
    best_setup = await pool.fetchval(
        'SELECT "SetupName" FROM paper_trades'
        ' WHERE ("EntryTime" AT TIME ZONE $2)::date = $1 AND "Pnl" IS NOT NULL'
        ' GROUP BY "SetupName" ORDER BY sum("Pnl") DESC NULLS LAST LIMIT 1;',
        session_date, IST_TZ,
    ) or ""

    pnls = [float(r["Pnl"]) for r in trades]
    wins = [p for p in pnls if p > 0]
    losses = [p for p in pnls if p < 0]
    total = len(pnls)
    gross = sum(pnls)
    win_rate = len(wins) / total if total else 0.0

    # avg R:R = mean win magnitude / mean loss magnitude (session level).
    avg_win = sum(wins) / len(wins) if wins else 0.0
    avg_loss = abs(sum(losses) / len(losses)) if losses else 0.0
    avg_rr = (avg_win / avg_loss) if avg_loss > 0 else (float(len(wins)) if wins else 0.0)

    # Max drawdown from the cumulative P&L curve (peak-to-trough).
    peak = 0.0
    cum = 0.0
    max_dd = 0.0
    for p in pnls:
        cum += p
        peak = max(peak, cum)
        max_dd = min(max_dd, cum - peak)

    # Rolling accuracy: today's win_rate blended with prior sessions (proxy until
    # the ML model reports its own accuracy; CLAUDE.md §5c — must trend up).
    prior = await pool.fetch(
        'SELECT "WinRate" FROM session_reports WHERE "SessionDate" < $1'
        ' ORDER BY "SessionDate" DESC LIMIT 4;', session_date,
    )
    prior_rates = [float(r["WinRate"]) for r in prior]
    rolling_accuracy = (sum(prior_rates) + win_rate) / (len(prior_rates) + 1)

    report = SessionReport(
        session_date=session_date, total_trades=total, wins=len(wins), losses=len(losses),
        no_trades=int(no_trades or 0), gross_pnl=round(gross, 2), max_drawdown=round(max_dd, 2),
        win_rate=round(win_rate, 4), avg_rr=round(avg_rr, 3),
        rolling_accuracy=round(rolling_accuracy, 4), best_setup=best_setup,
        notes="auto-generated at session end",
    )

    await pool.execute(
        'INSERT INTO session_reports ("SessionDate","TotalTrades","Wins","Losses","NoTrades",'
        '"GrossPnl","MaxDrawdown","WinRate","AvgRr","RollingAccuracy","BestSetup","Notes")'
        ' VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12)'
        ' ON CONFLICT ("SessionDate") DO UPDATE SET'
        ' "TotalTrades"=EXCLUDED."TotalTrades","Wins"=EXCLUDED."Wins","Losses"=EXCLUDED."Losses",'
        ' "NoTrades"=EXCLUDED."NoTrades","GrossPnl"=EXCLUDED."GrossPnl",'
        ' "MaxDrawdown"=EXCLUDED."MaxDrawdown","WinRate"=EXCLUDED."WinRate","AvgRr"=EXCLUDED."AvgRr",'
        ' "RollingAccuracy"=EXCLUDED."RollingAccuracy","BestSetup"=EXCLUDED."BestSetup",'
        ' "Notes"=EXCLUDED."Notes";',
        report.session_date, report.total_trades, report.wins, report.losses, report.no_trades,
        report.gross_pnl, report.max_drawdown, report.win_rate, report.avg_rr,
        report.rolling_accuracy, report.best_setup, report.notes,
    )
    return report
