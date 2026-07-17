"""Dhan API v2 adapter — OPTIONAL backup Greeks source (CLAUDE.md §1).

NOT built unless needed (Upstox covers Greeks). Kept as an interface-compatible
placeholder so switching the Greeks source is a config change, not a rewrite.
If ever used: Dhan forces a PULL (~1 option-chain request / 3s) — inferior to
the Upstox push, hence it is the backup, not the primary.
"""
from __future__ import annotations

from typing import AsyncIterator

from engine.config import Config
from engine.models.market import OptionChainSnapshot


class DhanConnector:
    """IGreeksProvider (backup only)."""

    def __init__(self, cfg: Config) -> None:
        self._cfg = cfg

    async def connect(self) -> None:
        ...

    async def stream_option_chain(
        self, underlying: str, expiry: str
    ) -> AsyncIterator[OptionChainSnapshot]:
        raise NotImplementedError("Dhan backup not built (Upstox is primary)")
        yield  # pragma: no cover
