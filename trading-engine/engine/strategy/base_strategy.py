"""Strategy base contract (CLAUDE.md §5c).

Strategies depend on the provider INTERFACES only, never a concrete broker.
The decision pipeline for every candle/second:
    premarket bias -> regime gate -> multiframe alignment -> signal
    -> risk_manager veto -> (paper/live) order -> hold_trail management
"""
from __future__ import annotations

from abc import ABC, abstractmethod

from engine.models.market import OptionChainSnapshot
from engine.models.orders import Signal


class BaseStrategy(ABC):
    @abstractmethod
    def on_chain(self, snapshot: OptionChainSnapshot) -> Signal | None:
        """Evaluate the latest state and return a Signal, or None to sit out."""
        raise NotImplementedError
