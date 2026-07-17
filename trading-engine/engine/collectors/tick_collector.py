"""Fan incoming ticks/candles to storage (CLAUDE.md §6).

Bridges a provider's async tick stream to the fire-and-forget DB/Redis writers.
Never blocks the hot path — writers own their own queues.
"""
from __future__ import annotations

from engine.connectors.interfaces import IMarketDataProvider
from engine.storage.redis_writer import RedisWriter
from engine.storage.timescale_writer import TimescaleWriter


class TickCollector:
    def __init__(
        self,
        provider: IMarketDataProvider,
        timescale: TimescaleWriter,
        redis: RedisWriter,
    ) -> None:
        self._provider = provider
        self._timescale = timescale
        self._redis = redis

    async def run(self, symbols: list[str]) -> None:
        async for tick in self._provider.stream_ticks(symbols):
            self._timescale.write_tick(tick)  # fire-and-forget
            await self._redis.set_snapshot(f"tick:{tick.symbol}", tick.__dict__)
