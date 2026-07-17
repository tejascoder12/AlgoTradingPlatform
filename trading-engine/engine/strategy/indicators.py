"""Technical indicators for the confluence strategy (pure functions, testable).

Everything operates on simple lists / Candle dataclasses — no external TA lib,
so the math is transparent and the same code can later run in backtests.
"""
from __future__ import annotations

from dataclasses import dataclass

from engine.models.market import Candle


# ----------------------------------------------------------------------
# Moving averages / oscillators
# ----------------------------------------------------------------------
def ema(values: list[float], period: int) -> list[float]:
    """Exponential moving average series (same length as input; seeds with SMA)."""
    if len(values) < period:
        return []
    k = 2.0 / (period + 1)
    out: list[float] = []
    sma = sum(values[:period]) / period
    out.extend([sma] * period)  # flat seed for the warmup region
    prev = sma
    for v in values[period:]:
        prev = v * k + prev * (1 - k)
        out.append(prev)
    return out


def rsi(closes: list[float], period: int = 14) -> float | None:
    """Wilder's RSI, last value only. None until enough data."""
    if len(closes) < period + 1:
        return None
    gains, losses = 0.0, 0.0
    for i in range(1, period + 1):
        d = closes[i] - closes[i - 1]
        gains += max(d, 0.0)
        losses += max(-d, 0.0)
    avg_gain, avg_loss = gains / period, losses / period
    for i in range(period + 1, len(closes)):
        d = closes[i] - closes[i - 1]
        avg_gain = (avg_gain * (period - 1) + max(d, 0.0)) / period
        avg_loss = (avg_loss * (period - 1) + max(-d, 0.0)) / period
    if avg_loss == 0:
        return 100.0
    rs = avg_gain / avg_loss
    return 100.0 - 100.0 / (1.0 + rs)


def atr(candles: list[Candle], period: int = 14) -> float | None:
    """Average True Range (Wilder). None until enough data."""
    if len(candles) < period + 1:
        return None
    trs: list[float] = []
    for i in range(1, len(candles)):
        c, p = candles[i], candles[i - 1]
        trs.append(max(c.high - c.low, abs(c.high - p.close), abs(c.low - p.close)))
    a = sum(trs[:period]) / period
    for tr in trs[period:]:
        a = (a * (period - 1) + tr) / period
    return a


# ----------------------------------------------------------------------
# Candle anatomy (the user's "candle showing upward/downward")
# ----------------------------------------------------------------------
@dataclass(slots=True)
class CandleRead:
    direction: str      # "bull" | "bear" | "doji"
    body_ratio: float   # |close-open| / (high-low), 0..1 — conviction of the candle


def read_candle(c: Candle) -> CandleRead:
    rng = c.high - c.low
    if rng <= 0:
        return CandleRead("doji", 0.0)
    body = abs(c.close - c.open) / rng
    if body < 0.3:
        return CandleRead("doji", body)
    return CandleRead("bull" if c.close > c.open else "bear", body)


# ----------------------------------------------------------------------
# Support / Resistance from swing points (fractals)
# ----------------------------------------------------------------------
def swing_levels(candles: list[Candle], k: int = 2) -> tuple[list[float], list[float]]:
    """Return (supports, resistances) = swing lows / swing highs.

    A swing low is a candle whose low is lower than the k candles on each side
    (mirror for swing highs). Sorted ascending.
    """
    supports: list[float] = []
    resistances: list[float] = []
    for i in range(k, len(candles) - k):
        lows_ok = all(candles[i].low <= candles[i + j].low and candles[i].low <= candles[i - j].low
                      for j in range(1, k + 1))
        highs_ok = all(candles[i].high >= candles[i + j].high and candles[i].high >= candles[i - j].high
                       for j in range(1, k + 1))
        if lows_ok:
            supports.append(candles[i].low)
        if highs_ok:
            resistances.append(candles[i].high)
    return sorted(supports), sorted(resistances)


def nearest_below(levels: list[float], price: float) -> float | None:
    """Nearest support under price."""
    below = [x for x in levels if x < price]
    return max(below) if below else None


def nearest_above(levels: list[float], price: float) -> float | None:
    """Nearest resistance over price."""
    above = [x for x in levels if x > price]
    return min(above) if above else None
