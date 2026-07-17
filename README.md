# AlgoTrading — AI-Assisted Options Trading Platform (Nifty 50)

A retail **algorithmic options-trading platform** for the Indian market (Nifty 50 index
options, intraday). It collects per-second market data, runs a rule-based **confluence
strategy** in a real-time engine, **paper-trades** it, logs every decision, and is built
to train an **AI/ML model** on that data to make the buy/sell/hold decisions in later phases.

> ⚠️ **Educational / research project. Paper trading only. Not financial advice.** See [LICENSE](LICENSE).

---

## 1. What it does

- **Collects** live Nifty option-chain data (LTP, Volume, OI, IV, Greeks) every few seconds into a time-series DB.
- **Analyzes** it with a multi-timeframe confluence strategy (trend + momentum + S/R + OI + volatility).
- **Paper-trades** the signals with realistic sizing, brokerage and risk limits (zero real orders).
- **Logs every decision** — entries, exits, and *why it sat out* — as a labelled dataset.
- **(Phase 2, planned)** trains an **XGBoost** model on that dataset to make the decisions.

### Phased plan
| Phase | Goal | Status |
|---|---|---|
| **1** | Paper trading + per-second data collection | ✅ **Active** |
| **2** | Train an AI/ML model on the collected data for buy/sell/hold | 🔜 Planned |
| **3** | Go live with low risk sizing + capital/profit calculator | ⏳ Future |

---

## 2. Architecture — hot path / cold path (never mixed)

The **trading engine** (hot path) and the **API + dashboard** (cold path) are fully isolated
processes that share **only the database and cache**. If the dashboard crashes, the engine keeps trading.

```
┌────────────────────────────────────────────────────────────────────────┐
│  DATA PROVIDERS (push, WebSocket)                                        │
│   • Angel One SmartAPI  → ticks, LTP, Volume, OI, OHLC  + ORDER EXEC     │
│   • Upstox (Plus)       → option chain + Greeks (DATA ONLY, zero orders) │
└───────────────┬────────────────────────────────────────────────────────┘
                │
        ┌───────▼─────────┐   writes only    ┌──────────────────────────┐
        │ Python Engine   │ ───────────────► │ TimescaleDB  +  Redis    │
        │ (HOT PATH)      │                  │ (time-series + live cache)│
        │ strategy + risk │                  └────────────┬─────────────┘
        └─────────────────┘                     reads only │
                                              ┌────────────▼─────────────┐
                                              │ ASP.NET Core Web API      │
                                              │ (COLD PATH, read-mostly)  │
                                              └────────────┬─────────────┘
                                                    SignalR │ HTTP
                                              ┌────────────▼─────────────┐
                                              │ Angular Dashboard         │
                                              └──────────────────────────┘
```

**Rules:** the engine only *writes*; the API/UI only *read*; the UI never calls the engine.
Only **Angel One** places orders — **Upstox is data-only** (so Plus stays free, no basis risk).

---

## 3. Tech stack

| Layer | Tech |
|---|---|
| Trading engine | **Python 3.11+**, asyncio |
| Time-series DB | **PostgreSQL + TimescaleDB** |
| Live cache | **Redis** (heartbeat / kill-switch / live snapshot) |
| Backend API | **ASP.NET Core (.NET 9)** — layered, repository pattern |
| Real-time push | **SignalR** |
| Dashboard | **Angular 19** (standalone components, signals) |
| Charts | lightweight-charts (TradingView) |
| ML (Phase 2) | XGBoost, pandas, scikit-learn |

---

## 4. Repository layout

```
AlgoTrading/
├── docker-compose.yml            # TimescaleDB + Redis (optional; local native also works)
├── AlgoTrading.sln
├── src/                          # ASP.NET Core (dependencies flow inward)
│   ├── AlgoTrading.Domain/       #   entities + enums (no dependencies)
│   ├── AlgoTrading.Repository/   #   EF Core DbContext, configs, repositories, Redis cache
│   ├── AlgoTrading.Business/     #   services, DTOs, mappers
│   └── AlgoTrading.API/          #   controllers, SignalR hub, DI (reads DB/Redis only)
├── tests/AlgoTrading.Tests/      # xUnit
└── trading-engine/               # Python real-time engine (the hot path)
    ├── main.py                   #   engine entrypoint (skeleton)
    ├── run_paper.py              #   the live paper-trading loop (the strategy)
    ├── smoke_test.py             #   broker + data connectivity check
    ├── start_engine.ps1          #   Task Scheduler launcher (logs to logs/)
    └── engine/
        ├── connectors/           #   broker adapters (Angel One, Upstox, ...) + interfaces
        ├── strategy/             #   indicators, confluence, OI, context, expiry rules
        ├── risk/                 #   risk_manager.py — the hard risk gate
        ├── storage/              #   async TimescaleDB + Redis writers
        ├── reporting/            #   end-of-session report
        └── ml/                   #   features / model / retrain (Phase 2 scaffold)

# Frontend lives separately at ../FE/Angular  (Angular 19 dashboard)
```

---

## 5. The strategy (confluence, intraday Nifty options)

The engine trades **only when multiple independent signals align** — and sits out otherwise.
Every gate below must pass to enter; a blocked entry is logged with the failing reason.

### Multi-timeframe
- **5-minute trend** (higher timeframe) via a **stacked EMA 9 / 15 / 20** —
  `direction` (EMA9 vs EMA20, responsive) + `strength` (fully stacked = strong).
- **1-minute** for precise entry timing (EMA15, RSI14, ATR, candle body).
- **Rule:** only take CE (long) when the 5m trend is up, PE (short) when down — never fight the higher timeframe.

### Entry gates (all must align)
1. **5m trend direction** (trade with it)
2. **Momentum** ≥ 0.6× ATR (adaptive, not a fixed number)
3. **Candle** direction with a real body (≥ 50%)
4. **RSI** in-band (50–72 for CE, 28–50 for PE — strong, not exhausted)
5. **S/R room** — don't buy *into* a level (needs ≥ 0.6× ATR of room)
6. **Volume flow** above its rolling average
7. **OI structure** — PCR + OI walls (support/resistance from open interest)
8. **Key levels** — previous day/week/month close + today's 09:15–09:30 opening-range high/low
9. **Time window** — high-liquidity windows, or a confirmed 5m trend + volume
10. **Risk gate** — final veto (per-trade cap, daily halt, max positions)

### Exit logic
- **Structure target** — the next resistance (CE) / support (PE), sized via option delta.
- **Profit-lock trail** — protects gains once up (beats whipsaws).
- **RUNNER mode** — on a *strong* 5m trend **or IV expansion**, ride for a **big target** with a wide trail (don't scalp a real trend small).
- **SCALP mode** — chop / weak trend: tight stop, quick target, fast theta exit.
- Plus: hard premium stop, RSI exhaustion, structure break, 5m-weakening, EOD square-off.

### Volatility / IV signal
- **IV expansion** — when the spot is flat but the **ATM straddle is rising**, premiums are building on vega → a big move is brewing → the next entry is promoted to a runner.

### Expiry-day rules (DTE = 0)
- 13:00–15:20 = strict scalp window (cheap premiums, sharp moves), faster exits, tighter theta cut.

### Risk management (dynamic)
- Per-trade risk **scaled by conviction (5m strength) × volatility (VIX) × remaining daily budget**.
- Position **sized from the risk budget** (`lots = risk_budget ÷ risk_per_lot`).
- Realistic **brokerage-aware P&L**, minimum net-profit floor.
- **Hard caps** (configurable in `.env`): per-trade %, daily loss halt, max open positions, no overnight holds.
- The risk manager is the **final authority** — it can veto any order.

---

## 6. Data storage (TimescaleDB schema)

Time-series tables are **TimescaleDB hypertables** (partitioned on time). Written only by the engine.

| Table | What it stores |
|---|---|
| `option_ticks` | per-second option snapshot (LTP, volume, OI, IV) — *hypertable* |
| `ohlc_candles` | 1m / 5m OHLC candles (built by the engine) — *hypertable* |
| `oi_snapshots` | OI per strike per minute — *hypertable* |
| `market_context` | per-candle regime (VIX, DTE, trend, bias) — *hypertable* |
| `strategy_signals` | **every decision** + full feature vector (JSON) + outcome (win/loss) |
| `no_trades` | every decision to **sit out**, with the failing gate (the discipline record) |
| `paper_trades` | simulated trades: entry/exit/qty/**net P&L**/setup context |
| `loss_attribution` | *why* a trade lost (the ML "goldmine") |
| `session_reports` | end-of-session summary + ML metrics (win rate, R:R, drawdown) |
| `engine_sessions` | one row per engine **run** (start/end/duration) for uptime tracking |

**The whole point:** `strategy_signals` + `no_trades` + `loss_attribution` form a growing,
fully-labelled dataset — *this is what the Phase-2 ML model trains on.*

---

## 7. Current status & testing

- **Phase 1 (paper trading) is live.** The engine connects to real broker data (Angel One + Upstox),
  collects per-second data, runs the confluence strategy, and paper-trades it end-to-end.
- **Zero real orders** — `PAPER_TRADING=true`. Fills and P&L are simulated (net of brokerage).
- **Unit tests:** Python (`pytest` — indicators, confluence, risk, expiry rules) and .NET (`xunit`).
- **Honest note:** the sample is still small (tens of trades) and the strategy is being calibrated —
  results so far are **not yet a proven edge**. Phase 1's real output is the **measured data**
  (win rate, R:R, drawdown), which will calibrate the honest target and feed the ML.

Run manually:
```bash
cd trading-engine
python run_paper.py          # runs until 15:20 IST square-off, Ctrl+C to stop
```

---

## 8. Future scope — AI / ML (Phase 2)

The system is built so the decision logic can move from **rules → a trained model**:

1. **Backfill** — pull expired-contract OHLC from Upstox Plus to bootstrap a historical dataset.
2. **Features** — the same context the strategy uses (DTE, VIX, OI/PCR, momentum/ATR, EMA stack, candle, S/R distance, IV expansion, opening range …) → `engine/ml/features.py`.
3. **Labels** — triple-barrier outcome (win / loss / no-trade) already captured in `strategy_signals` + `loss_attribution`.
4. **Model** — start with **XGBoost** (tabular, interpretable, small-data friendly); two heads:
   *trade filter* (trade / no-trade) and *direction* (CE / PE).
5. **Objective** — optimise **risk-adjusted return** (Sharpe / drawdown), not raw return.
6. **Nightly retrain** — after each session, retrain on (today + all prior) data; track rolling accuracy in `session_reports` (must trend up).
7. **Decision** — the model then produces buy/sell/hold with a confidence score; the **risk manager still has the final veto**.
8. **(Phase 4, parked)** positional/long-term equity with fundamentals + technicals.

The learning loop is deliberate: **the loss log is the goldmine — the model learns most from *why* it lost.**

---

## 9. Setup (quick start)

**Prerequisites:** .NET 9 SDK, Python 3.11+, Node 18+, PostgreSQL 16+ with the TimescaleDB extension
(or use the bundled `docker-compose.yml`), and Redis (optional until the live dashboard).

```bash
# 1. Database (option A: Docker)
docker-compose up -d

# 2. Backend API (.NET)
cd src/AlgoTrading.API
dotnet ef database update --project ../AlgoTrading.Repository --startup-project .
dotnet run                                   # Swagger at http://localhost:5014/swagger

# 3. Trading engine (Python)
cd ../../trading-engine
python -m venv .venv && .venv/Scripts/activate     # (Windows)
pip install -r requirements.txt
cp .env.example .env                          # then fill in broker keys — NEVER commit .env
python run_paper.py

# 4. Frontend (Angular, separate folder)
cd ../../FE/Angular
npm install && npm start                      # http://localhost:4200
```

Configuration (capital, risk %, broker keys) lives in `trading-engine/.env` — see `.env.example`.

---

## 10. License

[MIT](LICENSE) — free to use, modify and distribute. **Not financial advice; use at your own risk.**
