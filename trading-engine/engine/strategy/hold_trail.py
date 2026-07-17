"""'Hold for max profit' = TRAIL, not a fixed micro-target (CLAUDE.md §5c).

In a winning trade with clear structure: trail the stop behind structure and let
the trend run. EXIT immediately if structure breaks / volume dries up / HTF flips
/ target hit. Captures the big move while protecting locked-in profit.
STUB: replace with real structure detection (e.g. higher-lows for longs).
"""
from __future__ import annotations

from dataclasses import dataclass


@dataclass
class TrailState:
    trailing_stop: float
    exit_now: bool = False
    exit_reason: str = ""


def update_trail(
    current_price: float,
    last_stop: float,
    structure_low: float,
    structure_broken: bool,
    volume_dried_up: bool,
    htf_flipped: bool,
) -> TrailState:
    if structure_broken:
        return TrailState(last_stop, True, "structure_break")
    if volume_dried_up:
        return TrailState(last_stop, True, "volume_dry")
    if htf_flipped:
        return TrailState(last_stop, True, "htf_flip")
    # Ratchet the stop up behind structure; never move it down.
    new_stop = max(last_stop, structure_low)
    return TrailState(new_stop)
