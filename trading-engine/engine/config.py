"""Central config — loads secrets from .env via python-dotenv (CLAUDE.md §6).

NEVER hardcode API keys. Copy .env.example -> .env and fill in real values.
"""
from __future__ import annotations

import os
from dataclasses import dataclass

from dotenv import load_dotenv

load_dotenv()


def _get(name: str, default: str | None = None) -> str:
    value = os.getenv(name, default)
    if value is None:
        raise RuntimeError(f"Missing required env var: {name}")
    return value


@dataclass(frozen=True)
class RiskConfig:
    """Section 5d guardrails. Loaded once, never mutated at runtime."""
    starting_capital: float = float(os.getenv("STARTING_CAPITAL", "100000"))
    risk_per_trade_pct: float = float(os.getenv("RISK_PER_TRADE_PCT", "0.02"))   # regular-day 2% cap
    # Expiry-day per-trade risk budget (user chooses a higher appetite for scalping).
    # This EXCEEDS the §5d 2% guideline on purpose — it is a paper HYPOTHESIS to
    # measure; the daily loss halt below is the real backstop.
    expiry_risk_pct: float = float(os.getenv("EXPIRY_RISK_PCT", "0.04"))
    daily_loss_limit_pct: float = float(os.getenv("DAILY_LOSS_LIMIT_PCT", "0.05"))  # -5% halt
    max_open_positions: int = int(os.getenv("MAX_OPEN_POSITIONS", "3"))
    max_lots: int = int(os.getenv("MAX_LOTS", "5"))
    # Realistic cost modelling so paper P&L mirrors real net (brokerage + taxes,
    # round trip). ~Rs20/leg brokerage + STT/exchange/GST ≈ Rs50 round trip.
    round_trip_cost: float = float(os.getenv("ROUND_TRIP_COST", "50"))
    # Don't book a "profit" smaller than this NET of costs — it's not worth it.
    min_net_profit: float = float(os.getenv("MIN_NET_PROFIT", "100"))


@dataclass(frozen=True)
class Config:
    # --- Datastores (match docker-compose / appsettings) ---
    postgres_dsn: str = os.getenv(
        "POSTGRES_DSN",
        "postgresql://algo:algo_dev_pwd@localhost:5432/algotrading",
    )
    redis_url: str = os.getenv("REDIS_URL", "redis://localhost:6379/0")

    # --- Angel One (execution + ticks/OHLC/OI) ---
    angel_api_key: str = os.getenv("ANGEL_API_KEY", "")
    angel_client_id: str = os.getenv("ANGEL_CLIENT_ID", "")
    angel_pin: str = os.getenv("ANGEL_PIN", "")
    angel_totp_secret: str = os.getenv("ANGEL_TOTP_SECRET", "")

    # --- Upstox (PRIMARY Greeks/option-chain via WebSocket, data only) ---
    upstox_access_token: str = os.getenv("UPSTOX_ACCESS_TOKEN", "")

    # --- Trading mode ---
    paper_trading: bool = os.getenv("PAPER_TRADING", "true").lower() == "true"

    risk: RiskConfig = RiskConfig()


config = Config()
