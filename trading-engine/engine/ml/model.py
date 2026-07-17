"""XGBoost buy/sell/hold model (CLAUDE.md §5c, §6).

Optimizes for risk-adjusted return (Sharpe / consistency), NOT raw return (§5d).
STUB: wire real xgboost train/predict + model persistence.
"""
from __future__ import annotations

from engine.models.orders import SignalType


class TradingModel:
    def __init__(self, model_path: str | None = None) -> None:
        self._model = None
        self._model_path = model_path

    def load(self) -> None:
        # TODO: self._model = xgboost.Booster(); self._model.load_model(self._model_path)
        ...

    def predict(self, features: list[float]) -> tuple[SignalType, float]:
        """Return (decision, confidence). Falls back to HOLD until a model is trained."""
        if self._model is None:
            return SignalType.HOLD, 0.0
        raise NotImplementedError("XGBoost predict not yet wired")
