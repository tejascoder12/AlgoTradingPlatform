"""Redis live-snapshot writer + heartbeat (CLAUDE.md §2, §13).

The dashboard reads these keys to render LIVE state without touching the engine.
STUB: wire redis.asyncio for real writes.
"""
from __future__ import annotations

import json
import time

from engine.config import Config

HEARTBEAT_KEY = "engine:heartbeat"  # Unix-ms epoch; API reads this for LIVE/OFFLINE
KILL_SWITCH_KEY = "engine:kill"     # dashboard/phone sets this to halt the engine (§13)


class RedisWriter:
    def __init__(self, cfg: Config) -> None:
        self._cfg = cfg
        # self._redis = redis.asyncio.from_url(cfg.redis_url)  # TODO

    async def connect(self) -> None:
        ...

    async def heartbeat(self) -> None:
        """Write 'last alive' timestamp. Call every few seconds from the main loop."""
        _ = int(time.time() * 1000)
        # TODO: await self._redis.set(HEARTBEAT_KEY, _)

    async def set_snapshot(self, key: str, value: dict) -> None:
        _ = json.dumps(value)
        # TODO: await self._redis.set(key, _)

    async def is_kill_switch_set(self) -> bool:
        """Manual remote halt — checked each loop (§13 kill switch)."""
        # TODO: return bool(await self._redis.get(KILL_SWITCH_KEY))
        return False
