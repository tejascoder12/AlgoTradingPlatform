"""Angel One SmartAPI adapter — SOLE execution broker + ticks/OHLC/OI (CLAUDE.md §1).

Implements BOTH IMarketDataProvider and IOrderExecutor.
STUB: wire the real SmartAPI SDK + WebSocket here. Auth uses API key + client id
+ PIN + TOTP (see config). Order path MUST implement 429/rate-limit retry with
exponential backoff — rejected attempts also consume quota (CLAUDE.md §6).
"""
from __future__ import annotations

import asyncio
from typing import AsyncIterator

from engine.config import Config
from engine.models.market import Candle, Tick
from engine.models.orders import OrderRequest, OrderResult


class AngelOneConnector:
    """IMarketDataProvider + IOrderExecutor."""

    def __init__(self, cfg: Config) -> None:
        self._cfg = cfg
        self._connected = False

    # --- IMarketDataProvider ---
    async def connect(self) -> None:
        # TODO: authenticate (TOTP) and open the SmartAPI WebSocket.
        self._connected = True

    async def stream_ticks(self, symbols: list[str]) -> AsyncIterator[Tick]:
        # TODO: subscribe to symbols and yield real ticks from the socket.
        raise NotImplementedError("Angel One tick stream not yet implemented")
        yield  # pragma: no cover  (makes this an async generator)

    async def get_historical_ohlc(
        self, symbol: str, interval: str, start: str, end: str
    ) -> list[Candle]:
        raise NotImplementedError("Angel One historical OHLC not yet implemented")

    # --- IOrderExecutor ---
    async def place_order(self, request: OrderRequest) -> OrderResult:
        return await self._with_backoff(lambda: self._place_order_once(request))

    async def cancel_order(self, order_id: str) -> OrderResult:
        raise NotImplementedError

    async def flatten_all(self) -> list[OrderResult]:
        # TODO: query open positions and square them all off.
        return []

    # --- internals ---
    async def _place_order_once(self, request: OrderRequest) -> OrderResult:
        raise NotImplementedError("Angel One order placement not yet implemented")

    async def _with_backoff(self, fn, retries: int = 5) -> OrderResult:
        """Exponential backoff on 429 / rate-limit (CLAUDE.md §6)."""
        delay = 0.5
        for attempt in range(retries):
            try:
                return await fn()
            except NotImplementedError:
                raise
            except Exception:  # TODO: narrow to the SDK's rate-limit exception
                if attempt == retries - 1:
                    raise
                await asyncio.sleep(delay)
                delay *= 2
        return OrderResult(accepted=False, message="exhausted retries")
