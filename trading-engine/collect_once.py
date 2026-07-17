"""One-shot data collection proof (read-only on brokers, WRITES to TimescaleDB).

Fetches the live NIFTY option chain + Greeks from Upstox and writes each strike
(CE and PE) as an OptionTick row into the option_ticks hypertable. Run:
    python collect_once.py

This proves the full data path: Upstox -> engine models -> TimescaleDB.
The production engine will do this continuously over the Upstox WebSocket.
"""
from __future__ import annotations

import asyncio
from datetime import datetime, timezone

import httpx

from engine.config import config
from engine.models.market import OptionType, Tick
from engine.storage.timescale_writer import TimescaleWriter

NIFTY = "NSE_INDEX|Nifty 50"


def _fetch_chain() -> tuple[str, list[dict]]:
    headers = {"Authorization": f"Bearer {config.upstox_access_token}", "Accept": "application/json"}
    with httpx.Client(timeout=20) as client:
        rc = client.get("https://api.upstox.com/v2/option/contract",
                        headers=headers, params={"instrument_key": NIFTY})
        rc.raise_for_status()
        expiries = sorted({c["expiry"] for c in rc.json().get("data", []) if c.get("expiry")})
        nearest = expiries[0]

        r = client.get("https://api.upstox.com/v2/option/chain", headers=headers,
                       params={"instrument_key": NIFTY, "expiry_date": nearest})
        r.raise_for_status()
        return nearest, r.json().get("data", [])


def _to_ticks(chain: list[dict], now: datetime) -> list[Tick]:
    ticks: list[Tick] = []
    for row in chain:
        strike = int(row.get("strike_price", 0))
        for side, otype in (("call_options", OptionType.CE), ("put_options", OptionType.PE)):
            leg = row.get(side) or {}
            md = leg.get("market_data") or {}
            gk = leg.get("option_greeks") or {}
            ticks.append(Tick(
                time=now,
                symbol="NIFTY",
                strike=strike,
                option_type=otype,
                ltp=float(md.get("ltp") or 0.0),
                volume=int(md.get("volume") or 0),
                oi=int(md.get("oi") or 0),
                iv=float(gk.get("iv") or 0.0),
            ))
    return ticks


async def main() -> None:
    print("Fetching live NIFTY option chain from Upstox...")
    expiry, chain = _fetch_chain()
    now = datetime.now(timezone.utc)
    ticks = _to_ticks(chain, now)
    print(f"  expiry={expiry}  strikes={len(chain)}  option_tick rows={len(ticks)}")

    writer = TimescaleWriter(config)
    await writer.start()
    try:
        n = await writer.insert_option_ticks(ticks)
        print(f"  wrote {n} rows into option_ticks at {now.isoformat()}")
    finally:
        await writer.stop()
    print("Done.")


if __name__ == "__main__":
    asyncio.run(main())
