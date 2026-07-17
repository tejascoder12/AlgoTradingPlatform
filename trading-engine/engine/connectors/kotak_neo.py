"""Kotak Neo adapter — FAILOVER ONLY (CLAUDE.md §1).

Do not build core logic on this. Stub kept so a future failover path has a home.
"""
from __future__ import annotations

from engine.config import Config


class KotakNeoConnector:
    """Failover stub only."""

    def __init__(self, cfg: Config) -> None:
        self._cfg = cfg

    async def connect(self) -> None:
        raise NotImplementedError("Kotak Neo is a failover stub — not implemented")
