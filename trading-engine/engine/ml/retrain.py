"""End-of-session retrain + accuracy tracking (CLAUDE.md §5c, §12).

Runs as part of the 15:30 routine: retrain on (today's data) + (all prior).
The loss log (`loss_attribution`) is the goldmine — the model learns most from
WHY it lost. Track rolling accuracy in session_reports; it must trend UP.
STUB: wire the real training pipeline.
"""
from __future__ import annotations


def retrain_nightly() -> float:
    """Retrain and return the new rolling accuracy. TODO: real implementation."""
    # 1. Load labeled decisions (features + win/loss/no-trade) from the DB.
    # 2. Weight heavily on loss_attribution rows.
    # 3. Fit XGBoost, evaluate, persist the model.
    # 4. Return rolling accuracy for session_reports.
    raise NotImplementedError("nightly retrain not yet implemented")
