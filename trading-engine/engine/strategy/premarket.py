"""Pre-market reading before 09:15 (CLAUDE.md §5c).

Estimate the open (gap up / flat / gap down) from global cues, GIFT Nifty and
overnight news; strong/bad news sets the day's directional bias.
Output: a `premarket_bias` value stored in market_context.
STUB: wire the real data sources (GIFT Nifty quote, news sentiment).
"""
from __future__ import annotations

from enum import Enum


class PremarketBias(str, Enum):
    GAP_UP = "gap_up"
    FLAT = "flat"
    GAP_DOWN = "gap_down"


def estimate_bias(gift_nifty_change_pct: float) -> PremarketBias:
    if gift_nifty_change_pct > 0.3:
        return PremarketBias.GAP_UP
    if gift_nifty_change_pct < -0.3:
        return PremarketBias.GAP_DOWN
    return PremarketBias.FLAT
