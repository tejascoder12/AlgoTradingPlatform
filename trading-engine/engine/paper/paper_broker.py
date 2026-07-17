"""Simulated order fills + P&L for Phase 1 paper trading (CLAUDE.md §6).

Mimics IOrderExecutor so the same strategy code runs in paper and live.
Fills at the last price (extend with slippage/spread modelling later — live
result is typically 40-60% of paper, §5d).
"""
from __future__ import annotations

from datetime import datetime, timezone

from engine.models.orders import OrderRequest, OrderResult, TradeSide


class PaperBroker:
    """IOrderExecutor-compatible simulator."""

    def __init__(self) -> None:
        self._next_id = 1
        self._open: dict[str, tuple[TradeSide, int, float]] = {}

    async def place_order(self, request: OrderRequest, mark_price: float) -> OrderResult:
        order_id = str(self._next_id)
        self._next_id += 1
        fill = request.limit_price or mark_price
        self._open[order_id] = (request.side, request.qty, fill)
        return OrderResult(True, order_id=order_id, filled_price=fill, message="paper fill")

    async def flatten_all(self, mark_price: float) -> list[OrderResult]:
        results: list[OrderResult] = []
        for order_id, (side, qty, entry) in list(self._open.items()):
            pnl = (mark_price - entry) * qty * (1 if side == TradeSide.BUY else -1)
            results.append(OrderResult(True, order_id=order_id, filled_price=mark_price,
                                       message=f"flattened pnl={pnl:.2f}"))
            del self._open[order_id]
        return results

    @staticmethod
    def _now() -> datetime:
        return datetime.now(timezone.utc)
