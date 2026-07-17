"""Multi-timeframe alignment — the core edge (CLAUDE.md §5c).

HTF (15m/1h) gives the dominant trend; LTF (1m/3m/5m) gives precise entry timing.
RULE: only trade when HTF trend and LTF trigger ALIGN.
STUB: compute real trend/trigger from candle series.
"""
from __future__ import annotations


def is_aligned(htf_trend: str, ltf_trigger: str) -> bool:
    """True only when the lower-timeframe trigger agrees with the higher-timeframe trend."""
    return (
        (htf_trend == "up" and ltf_trigger == "breakout")
        or (htf_trend == "down" and ltf_trigger == "breakdown")
    )
