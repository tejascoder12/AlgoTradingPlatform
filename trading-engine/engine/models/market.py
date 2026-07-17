"""Market data models (dataclasses). Mirror the DB schema in CLAUDE.md §7."""
from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime
from enum import Enum


class OptionType(str, Enum):
    CE = "CE"  # Call
    PE = "PE"  # Put


@dataclass(slots=True)
class Tick:
    """Raw per-second snapshot of one option contract -> option_ticks table."""
    time: datetime
    symbol: str
    strike: int
    option_type: OptionType
    ltp: float
    volume: int
    oi: int
    iv: float


@dataclass(slots=True)
class Candle:
    """OHLC aggregate -> ohlc_candles table."""
    time: datetime
    symbol: str
    interval: str  # "1m" | "5m" | "15m"
    open: float
    high: float
    low: float
    close: float
    volume: int


@dataclass(slots=True)
class Greeks:
    delta: float
    gamma: float
    theta: float
    vega: float
    iv: float


@dataclass(slots=True)
class OptionChainRow:
    strike: int
    option_type: OptionType
    ltp: float
    oi: int
    oi_change: int
    greeks: Greeks


@dataclass(slots=True)
class OptionChainSnapshot:
    """A full option-chain push from Upstox (§1: PRIMARY Greeks source)."""
    time: datetime
    underlying: str
    expiry: str
    rows: list[OptionChainRow] = field(default_factory=list)
