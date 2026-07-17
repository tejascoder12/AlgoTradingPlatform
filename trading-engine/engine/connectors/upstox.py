"""Upstox adapter — PRIMARY Greeks / option-chain provider (CLAUDE.md §1).

DATA ONLY. Implements IMarketDataProvider / IGreeksProvider. NEVER implements
IOrderExecutor — Upstox places zero orders (that's why Plus is free for us).

Greeks arrive as a PUSH over the WebSocket `option_chain` mode — do NOT poll
(CLAUDE.md §10). Upstox Plus perks: 5 sockets, expired-contract OHLC (Phase 2
backfill), Depth 30.

API reference: https://upstox.com/developer/api-documentation/api-overview
STUB: wire the real Upstox WebSocket + REST here.
"""
from __future__ import annotations

from typing import AsyncIterator

from engine.config import Config
from engine.models.market import Candle, OptionChainSnapshot, Tick


class UpstoxConnector:
    """IMarketDataProvider + IGreeksProvider (data only)."""

    def __init__(self, cfg: Config) -> None:
        self._cfg = cfg

    async def connect(self) -> None:
        # TODO: open the Upstox WebSocket using the access token from config.
        ...

    # --- IGreeksProvider (the reason we use Upstox) ---
    async def stream_option_chain(
        self, underlying: str, expiry: str
    ) -> AsyncIterator[OptionChainSnapshot]:
        # TODO: subscribe in `option_chain` mode; yield full chain + Greeks per push.
        raise NotImplementedError("Upstox option-chain stream not yet implemented")
        yield  # pragma: no cover

    # --- IMarketDataProvider ---
    async def stream_ticks(self, symbols: list[str]) -> AsyncIterator[Tick]:
        raise NotImplementedError
        yield  # pragma: no cover

    async def get_historical_ohlc(
        self, symbol: str, interval: str, start: str, end: str
    ) -> list[Candle]:
        # Upstox Plus can return EXPIRED-contract OHLC — used to backfill Phase 2 training data.
        raise NotImplementedError("Upstox expired-contract OHLC not yet implemented")
