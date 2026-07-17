"""Build the feature vector from the context inputs (CLAUDE.md §5c).

Required feature set for every decision: DTE, Nifty futures, India VIX, OI + OI
change per strike, volume per candle, pre-market bias, breakout/breakdown levels.
STUB: assemble a real numeric vector for the model.
"""
from __future__ import annotations

from dataclasses import dataclass


@dataclass
class FeatureVector:
    dte: int
    vix: float
    volume_vs_avg: float
    oi_change: float
    premarket_bias: str
    htf_trend: str
    ltf_trigger: str

    def to_row(self) -> list[float]:
        """Numeric encoding for XGBoost. TODO: proper categorical encoding."""
        bias_map = {"gap_down": -1.0, "flat": 0.0, "gap_up": 1.0}
        trend_map = {"down": -1.0, "flat": 0.0, "up": 1.0}
        trig_map = {"breakdown": -1.0, "none": 0.0, "breakout": 1.0}
        return [
            float(self.dte),
            self.vix,
            self.volume_vs_avg,
            self.oi_change,
            bias_map.get(self.premarket_bias, 0.0),
            trend_map.get(self.htf_trend, 0.0),
            trig_map.get(self.ltf_trigger, 0.0),
        ]
