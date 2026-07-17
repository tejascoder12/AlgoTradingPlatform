"""Trading engine entrypoint (CLAUDE.md §2, §6, §13).

This is the HOT PATH process — no HTTP server, no UI. It only WRITES to
TimescaleDB + Redis; the API/dashboard only READ. If the dashboard crashes,
this keeps trading.

Startup self-check (§13): verify broker auth, DB and Redis BEFORE market open.
Run:  python main.py
"""
from __future__ import annotations

import asyncio
import logging

from engine.config import config
from engine.connectors.angel_one import AngelOneConnector
from engine.connectors.upstox import UpstoxConnector
from engine.risk.risk_manager import RiskManager
from engine.storage.redis_writer import RedisWriter
from engine.storage.timescale_writer import TimescaleWriter

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("engine")

HEARTBEAT_INTERVAL_SEC = 3  # §13 heartbeat cadence


async def run() -> None:
    logger.info("Starting engine (paper_trading=%s)", config.paper_trading)

    # --- Wire dependencies (strategy depends on interfaces, not concrete brokers) ---
    market = AngelOneConnector(config)     # ticks/OHLC/OI + execution
    greeks = UpstoxConnector(config)       # option chain + Greeks (data only)
    timescale = TimescaleWriter(config)
    redis = RedisWriter(config)
    risk = RiskManager(config.risk)        # final authority (§5d)

    # --- Startup self-check (§13) ---
    await timescale.start()
    await redis.connect()
    await market.connect()
    await greeks.connect()
    logger.info("Startup self-check complete")

    # --- Main loop skeleton ---
    try:
        while True:
            if await redis.is_kill_switch_set():
                logger.warning("Kill switch set — halting engine")
                break
            if risk.is_halted():
                logger.warning("Daily loss limit hit — no more entries today")
            await redis.heartbeat()
            # TODO: consume tick + option-chain streams, run strategy pipeline,
            #       pass every order through risk.check(), fill via paper/live broker.
            await asyncio.sleep(HEARTBEAT_INTERVAL_SEC)
    finally:
        await timescale.stop()
        logger.info("Engine stopped")


if __name__ == "__main__":
    asyncio.run(run())
