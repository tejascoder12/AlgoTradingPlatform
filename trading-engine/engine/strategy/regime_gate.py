"""No-trade / regime gate (CLAUDE.md §5c).

Checked BEFORE every entry. ALL must pass or we SIT OUT and log to `no_trades`.
Sitting out is a measured outcome — avoiding a loss counts as edge.
    - candle volume > rolling average volume
    - clear directional move (not oscillating in a tight band)
    - inside a high-liquidity time window
"""
from __future__ import annotations

from dataclasses import dataclass
from datetime import time

# High-liquidity windows (IST). Outside these, bias strongly toward no-trade.
# Closing window runs to 15:20 — if a real trend is running into the close it's
# tradeable, but everyone is flat by 15:20 regardless (see expiry_rules.SQUARE_OFF_TIME).
LIQUID_WINDOWS = [
    (time(9, 15), time(9, 30)),    # opening volume burst
    (time(12, 0), time(14, 0)),    # midday momentum
    (time(15, 0), time(15, 20)),   # closing move
]


@dataclass
class GateResult:
    allowed: bool
    reason: str  # "chop" | "low_volume" | "outside_window" | "dte_risk" | "ok"


def check_gate(
    now: time,
    volume_vs_avg: float,
    is_directional: bool,
    dte: int,
) -> GateResult:
    if not _in_liquid_window(now):
        return GateResult(False, "outside_window")
    if volume_vs_avg <= 1.0:
        return GateResult(False, "low_volume")
    if not is_directional:
        return GateResult(False, "chop")
    return GateResult(True, "ok")


def _in_liquid_window(now: time) -> bool:
    return any(start <= now <= end for start, end in LIQUID_WINDOWS)
