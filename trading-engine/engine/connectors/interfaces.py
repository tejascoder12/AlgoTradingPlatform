"""Broker/provider abstractions (CLAUDE.md §6).

Strategy code depends on THESE interfaces, never on a concrete broker class.
That way swapping the Greeks source (Upstox <-> Dhan) is a config change, not a rewrite.

Role split (FINALIZED — do not mix):
  - Angel One  -> IMarketDataProvider + IOrderExecutor  (SOLE execution broker)
  - Upstox     -> IMarketDataProvider / IGreeksProvider  (DATA ONLY, zero orders)
  - Dhan       -> IGreeksProvider (optional backup, not built unless needed)
  - Kotak Neo  -> failover stub only
"""
from __future__ import annotations

from typing import Protocol, AsyncIterator, runtime_checkable

from engine.models.market import Tick, OptionChainSnapshot, Candle
from engine.models.orders import OrderRequest, OrderResult


@runtime_checkable
class IMarketDataProvider(Protocol):
    """Live price/volume/OI feed + historical OHLC."""

    async def connect(self) -> None:
        """Open the underlying WebSocket / session."""
        ...

    async def stream_ticks(self, symbols: list[str]) -> AsyncIterator[Tick]:
        """Yield ticks (LTP, volume, OI) as they arrive over the socket."""
        ...

    async def get_historical_ohlc(
        self, symbol: str, interval: str, start: str, end: str
    ) -> list[Candle]:
        """Historical candles (Angel One live; Upstox expired-contract OHLC for backfill)."""
        ...


@runtime_checkable
class IGreeksProvider(Protocol):
    """Option chain + Greeks (Delta, Gamma, Theta, Vega, IV).

    PUSH via WebSocket `option_chain` mode — do NOT poll (CLAUDE.md §10).
    """

    async def connect(self) -> None: ...

    async def stream_option_chain(
        self, underlying: str, expiry: str
    ) -> AsyncIterator[OptionChainSnapshot]:
        """Continuously yield the full chain + Greeks for one underlying/expiry."""
        ...


@runtime_checkable
class IOrderExecutor(Protocol):
    """Order placement. ONLY Angel One implements this (CLAUDE.md §10).

    Implementations MUST do 429 / rate-limit retry with exponential backoff —
    rejected attempts also consume quota, so a burst of rejects can cascade.
    """

    async def place_order(self, request: OrderRequest) -> OrderResult: ...

    async def cancel_order(self, order_id: str) -> OrderResult: ...

    async def flatten_all(self) -> list[OrderResult]:
        """Force-close every open position (auto square-off / kill switch)."""
        ...
