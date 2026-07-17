"""Order + signal models (dataclasses)."""
from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from enum import Enum
from typing import Optional


class TradeSide(str, Enum):
    BUY = "Buy"
    SELL = "Sell"


class SignalType(str, Enum):
    HOLD = "Hold"
    BUY = "Buy"
    SELL = "Sell"


@dataclass(slots=True)
class OrderRequest:
    symbol: str
    side: TradeSide
    qty: int
    # None => market order; a value => limit order.
    limit_price: Optional[float] = None


@dataclass(slots=True)
class OrderResult:
    accepted: bool
    order_id: Optional[str] = None
    filled_price: Optional[float] = None
    message: str = ""


@dataclass(slots=True)
class Signal:
    """A strategy/ML decision -> strategy_signals table."""
    time: datetime
    signal_type: SignalType
    confidence: float
    features: dict
    reason: str = ""
