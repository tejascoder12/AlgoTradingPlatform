"""Connection + data smoke test (read-only, places ZERO orders).

Validates that the keys in .env actually work and that market data flows,
BEFORE we wire the full engine. Run:
    python smoke_test.py

Checks:
  1. Angel One SmartAPI login via TOTP -> pulls NIFTY historical candles.
  2. Upstox token -> profile + option-chain fetch (Greeks source).
  3. Postgres/TimescaleDB reachable.
"""
from __future__ import annotations

import sys
from datetime import datetime, timedelta

import httpx
import pyotp
from SmartApi import SmartConnect

from engine.config import config


def ok(msg: str) -> None:
    print(f"  [OK]   {msg}")


def fail(msg: str) -> None:
    print(f"  [FAIL] {msg}")


# ----------------------------------------------------------------------
# 1. Angel One — login + historical candles
# ----------------------------------------------------------------------
def test_angel_one() -> None:
    print("\n== Angel One SmartAPI ==")
    if not config.angel_api_key or not config.angel_totp_secret:
        fail("ANGEL_* keys not set in .env")
        return
    try:
        smart = SmartConnect(api_key=config.angel_api_key)
        totp = pyotp.TOTP(config.angel_totp_secret).now()
        session = smart.generateSession(config.angel_client_id, config.angel_pin, totp)
        if not session.get("status"):
            fail(f"login rejected: {session.get('message')}")
            return
        ok(f"login success (feed token acquired)")

        profile = smart.getProfile(session["data"]["refreshToken"])
        name = profile.get("data", {}).get("name", "?")
        ok(f"profile: {name}")

        # NIFTY 50 index token on NSE = 99926000. Pull last few days of 1-day candles.
        to_dt = datetime.now()
        from_dt = to_dt - timedelta(days=7)
        params = {
            "exchange": "NSE",
            "symboltoken": "99926000",
            "interval": "ONE_DAY",
            "fromdate": from_dt.strftime("%Y-%m-%d %H:%M"),
            "todate": to_dt.strftime("%Y-%m-%d %H:%M"),
        }
        candles = smart.getCandleData(params)
        rows = candles.get("data") or []
        if rows:
            ok(f"historical candles pulled: {len(rows)} rows; latest close={rows[-1][4]}")
        else:
            fail(f"no candle data returned: {candles.get('message')}")
    except Exception as e:  # noqa: BLE001
        fail(f"exception: {e}")


# ----------------------------------------------------------------------
# 2. Upstox — profile + option chain
# ----------------------------------------------------------------------
def test_upstox() -> None:
    print("\n== Upstox (Greeks / option chain) ==")
    if not config.upstox_access_token:
        fail("UPSTOX_ACCESS_TOKEN not set in .env")
        return
    headers = {
        "Authorization": f"Bearer {config.upstox_access_token}",
        "Accept": "application/json",
    }
    try:
        with httpx.Client(timeout=15) as client:
            # 2a. Profile — cheapest call to validate the token.
            r = client.get("https://api.upstox.com/v2/user/profile", headers=headers)
            if r.status_code == 200:
                name = r.json().get("data", {}).get("user_name", "?")
                ok(f"token valid; user: {name}")
            else:
                fail(f"profile call {r.status_code}: {r.text[:160]}")
                return

            # 2b. First fetch the REAL available expiries for NIFTY.
            nifty = "NSE_INDEX|Nifty 50"
            rc = client.get(
                "https://api.upstox.com/v2/option/contract",
                headers=headers,
                params={"instrument_key": nifty},
            )
            if rc.status_code != 200:
                fail(f"contract lookup {rc.status_code}: {rc.text[:200]}")
                return
            contracts = rc.json().get("data") or []
            expiries = sorted({c.get("expiry") for c in contracts if c.get("expiry")})
            if not expiries:
                fail("no expiries returned for NIFTY")
                return
            nearest = expiries[0]
            ok(f"available expiries: {expiries[:4]}... using nearest={nearest}")

            # 2c. Now pull the option chain + Greeks for the nearest expiry.
            r = client.get(
                "https://api.upstox.com/v2/option/chain",
                headers=headers,
                params={"instrument_key": nifty, "expiry_date": nearest},
            )
            if r.status_code == 200:
                data = r.json().get("data") or []
                ok(f"option chain rows: {len(data)}")
                if data:
                    mid = data[len(data) // 2]  # near ATM
                    ce = mid.get("call_options", {}).get("option_greeks", {})
                    ok(f"ATM~ strike {mid.get('strike_price')} CE greeks: "
                       f"delta={ce.get('delta')} theta={ce.get('theta')} iv={ce.get('iv')}")
            else:
                fail(f"option-chain {r.status_code}: {r.text[:200]}")
    except Exception as e:  # noqa: BLE001
        fail(f"exception: {e}")


def _next_thursday() -> str:
    """Nifty weekly options expire Thursday; return the next one as YYYY-MM-DD."""
    today = datetime.now().date()
    days = (3 - today.weekday()) % 7  # Thursday = 3
    return (today + timedelta(days=days)).strftime("%Y-%m-%d")


# ----------------------------------------------------------------------
# 3. Postgres / TimescaleDB reachable
# ----------------------------------------------------------------------
def test_postgres() -> None:
    print("\n== Postgres / TimescaleDB ==")
    try:
        import psycopg  # optional; only if installed
    except ImportError:
        print("  [skip] psycopg not installed — DB check skipped (engine uses asyncpg later)")
        return
    try:
        with psycopg.connect(config.postgres_dsn.replace("postgresql://", "postgresql://")) as conn:
            with conn.cursor() as cur:
                cur.execute("SELECT count(*) FROM timescaledb_information.hypertables;")
                ok(f"connected; hypertables = {cur.fetchone()[0]}")
    except Exception as e:  # noqa: BLE001
        fail(f"exception: {e}")


if __name__ == "__main__":
    print("=" * 60)
    print(f"SMOKE TEST  (paper_trading={config.paper_trading}) — read-only, no orders")
    print("=" * 60)
    test_angel_one()
    test_upstox()
    test_postgres()
    print("\nDone.")
    sys.exit(0)
