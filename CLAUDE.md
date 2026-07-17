# CLAUDE.md — Algo Trading Platform (Build Guide)

> This file is the source of truth for Claude Code. Read it fully before generating any code.
> Build **one layer at a time**, in the order given. Do not scaffold everything at once.

---

## 1. What We Are Building

A retail algorithmic options-trading platform for the Indian market (Nifty / BankNifty F&O).

**Phased plan:**
1. **Phase 1 (Month 1):** Paper trading + per-second data collection into a time-series DB.
2. **Phase 2:** Train an AI/ML model on collected data for buy/sell/hold decisions.
3. **Phase 3:** Go live with low risk sizing + a capital/profit calculator.

**Broker roles (FINALIZED — do not mix these responsibilities):**

| Broker | Role | Implements |
|---|---|---|
| **Angel One SmartAPI** | **SOLE EXECUTION broker** + ticks, LTP, Volume, OI, historical OHLC | `IMarketDataProvider` **and** `IOrderExecutor` |
| **Upstox (Plus plan)** | **PRIMARY DATA / GREEKS provider** — option chain + Greeks via WebSocket push; expired-contract OHLC for backfill | `IMarketDataProvider` / `IGreeksProvider` **ONLY** |
| **Kotak Neo** | FAILOVER only. Do not build core logic on it. | (adapter stub only) |
| **Dhan API v2** | OPTIONAL backup Greeks source. **Not built unless needed** (Upstox covers Greeks). | — |

**Critical separation rules:**
- **Only Angel One implements `IOrderExecutor`.** Upstox is DATA ONLY — never put order logic in the Upstox adapter.
- We act on Angel One's book *and* execute on Angel One's book → no basis risk. Greeks/OI from Upstox carry no basis risk (they don't drive the fill price).
- **Upstox Plus is free for us** because we place zero orders on Upstox; the Plus brokerage rates never apply. We keep the API perks: 5 WebSocket sockets, expired-contract OHLC, Depth 30.
- **Greeks feed is PUSH (Upstox WebSocket `option_chain` mode), not poll.** This fits the hot path natively — no 3s staleness, no per-request rate-limit babysitting.

**Why Upstox WebSocket over Dhan REST:** Dhan forces a pull (1 option-chain request / 3s = up to 3s stale Greeks, bolted onto a push system). Upstox streams the full chain + Greeks continuously over one socket. Cleaner hot path, fresher data.

---

## 2. The One Architecture Rule That Must Never Be Broken

**HOT PATH and COLD PATH must be fully isolated processes that share nothing but the database/cache.**

```
HOT PATH (Python engine — must never block)     COLD PATH (API + UI — can be slow)
WebSocket tick → strategy → order               Dashboard reads DB / Redis
   → async write to DB (fire-and-forget)         Charts render, P&L shown
```

**Two PUSH streams feed the engine (both WebSocket, no polling):**
```
Angel One WebSocket  ──┐  (ticks, LTP, Volume, OI, OHLC)
                       ├──►  Python engine (hot path) ──► TimescaleDB + Redis
Upstox WebSocket     ──┘  (option chain + Greeks: Delta, Gamma, Theta, Vega, IV)
```
Order execution goes back out through the **Angel One** adapter only.

- The **trading engine ONLY writes** data out (to TimescaleDB + Redis).
- The **API and dashboard ONLY read** (from TimescaleDB + Redis).
- The API/UI must NEVER call into the trading engine.
- If the dashboard or API crashes, the engine keeps trading. This isolation is mandatory.

They run as **separate processes**:
```
Process 1: Python trading engine   (no UI, no HTTP server)
Process 2: ASP.NET Core Web API    (reads DB/Redis only)
Process 3: Angular dashboard       (talks to API only)
```

---

## 3. Tech Stack

| Layer            | Tech                                   |
|------------------|----------------------------------------|
| Trading engine   | Python 3.11+ , asyncio                 |
| Time-series DB   | PostgreSQL + TimescaleDB extension     |
| Live cache       | Redis                                  |
| Backend API      | ASP.NET Core Web API (.NET 8 LTS)      |
| Real-time push   | SignalR                                |
| Dashboard        | Angular (latest), standalone components|
| Charts           | lightweight-charts (TradingView free)  |

---

## 4. ASP.NET Core — Solution Structure (Repository Pattern, Layered)

A single solution with **4 projects**. Dependencies flow inward only:
`API → Business → Repository → Domain`. Domain depends on nothing.

```
AlgoTrading.sln
│
├── src/
│   ├── AlgoTrading.API/                 # Web API — controllers + DI wiring ONLY
│   │   ├── Controllers/
│   │   │   ├── DashboardController.cs
│   │   │   ├── TradesController.cs
│   │   │   └── MarketDataController.cs
│   │   ├── Hubs/                         # SignalR hubs
│   │   │   └── LiveFeedHub.cs
│   │   ├── Middleware/
│   │   │   └── ExceptionMiddleware.cs
│   │   ├── Extensions/
│   │   │   └── ServiceCollectionExtensions.cs   # DI registration kept here
│   │   ├── appsettings.json
│   │   └── Program.cs
│   │
│   ├── AlgoTrading.Business/            # Business logic / services
│   │   ├── Interfaces/                  # IXxxService contracts
│   │   │   ├── ITradeService.cs
│   │   │   └── IMarketDataService.cs
│   │   ├── Services/                    # implementations
│   │   │   ├── TradeService.cs
│   │   │   └── MarketDataService.cs
│   │   ├── DTOs/                        # request/response objects (NOT entities)
│   │   │   ├── TradeDto.cs
│   │   │   └── PnlSummaryDto.cs
│   │   └── Mappers/                     # entity <-> DTO mapping
│   │
│   ├── AlgoTrading.Repository/          # Data access layer ONLY
│   │   ├── Interfaces/                  # IXxxRepository contracts
│   │   │   ├── IGenericRepository.cs
│   │   │   ├── ITradeRepository.cs
│   │   │   └── IMarketDataRepository.cs
│   │   ├── Repositories/                # implementations
│   │   │   ├── GenericRepository.cs
│   │   │   ├── TradeRepository.cs
│   │   │   └── MarketDataRepository.cs
│   │   ├── Context/
│   │   │   └── AppDbContext.cs          # EF Core DbContext
│   │   ├── Cache/
│   │   │   └── RedisCacheService.cs     # reads live snapshot from Redis
│   │   └── Configurations/              # EF entity type configs
│   │
│   └── AlgoTrading.Domain/             # Entities + shared interfaces. No dependencies.
│       ├── Entities/
│       │   ├── OptionTick.cs
│       │   ├── OhlcCandle.cs
│       │   ├── OiSnapshot.cs
│       │   ├── PaperTrade.cs
│       │   └── StrategySignal.cs
│       └── Enums/
│           ├── TradeSide.cs
│           └── SignalType.cs
│
└── tests/
    └── AlgoTrading.Tests/
```

### Rules for the API side
- Controllers are **thin**: validate input, call a service, return result. No business logic, no DB calls.
- Business services depend on **repository interfaces**, never on `AppDbContext` directly.
- Repositories return **entities**; services map entities to **DTOs** before returning to controllers.
- Use a **`IGenericRepository<T>`** base for CRUD, with specific repositories for custom queries.
- Register everything via `ServiceCollectionExtensions` in the API project — keep `Program.cs` clean.
- API is **read-mostly** in this app (it reads what the Python engine wrote). Keep write endpoints minimal.
- Use async/await everywhere (`Task<T>`), `CancellationToken` on all repo methods.
- Use `IConfiguration` for connection strings; never hardcode. Connection strings live in `appsettings.json` / user-secrets.

### IGenericRepository contract (target shape)
```csharp
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

---

## 5. Angular — Folder Structure (latest, standalone components)

Use **standalone components** (no NgModules), **signals** where it helps, and the modern `inject()` function.

```
src/
├── app/
│   ├── core/                       # singletons, loaded once
│   │   ├── services/               # app-wide services
│   │   │   └── signalr.service.ts  # live feed connection
│   │   ├── interceptors/
│   │   │   ├── auth.interceptor.ts
│   │   │   └── error.interceptor.ts
│   │   ├── guards/
│   │   └── config/
│   │       └── api-endpoints.ts     # central list of endpoint URLs
│   │
│   ├── shared/                     # reusable across features
│   │   ├── components/             # dumb/presentational components
│   │   ├── pipes/                  # helper pipes (currency-inr, pnl-color, etc.)
│   │   ├── directives/
│   │   └── models/                 # interfaces — match API DTOs
│   │       ├── trade.model.ts
│   │       ├── pnl-summary.model.ts
│   │       └── market-data.model.ts
│   │
│   ├── features/                   # one folder per screen/feature
│   │   ├── dashboard/
│   │   │   ├── dashboard.component.ts
│   │   │   ├── dashboard.component.html
│   │   │   ├── dashboard.component.scss
│   │   │   └── services/
│   │   │       └── dashboard.service.ts   # calls dashboard API endpoints
│   │   ├── trades/
│   │   │   ├── trades.component.ts
│   │   │   └── services/
│   │   │       └── trades.service.ts
│   │   └── charts/
│   │       ├── charts.component.ts
│   │       └── services/
│   │           └── chart-data.service.ts
│   │
│   ├── app.component.ts
│   ├── app.config.ts               # providers (HttpClient, router, etc.)
│   └── app.routes.ts               # lazy-loaded routes
│
├── environments/
│   ├── environment.ts
│   └── environment.development.ts
└── styles.scss
```

### Rules for the Angular side
- **One service per feature** for its API endpoints (e.g. `TradesService` calls only trade endpoints).
- All endpoint URLs centralized in `core/config/api-endpoints.ts` — never hardcode URLs in services.
- **Models = interfaces** that mirror the API DTOs one-to-one.
- Routing defined in `app.routes.ts` with **lazy loading** per feature: `loadComponent: () => import(...)`.
- HTTP via `HttpClient` injected with `inject()`; return `Observable<T>` typed with the model interface.
- Use the `error.interceptor.ts` for global error handling so components stay clean.
- Helper transformations (₹ formatting, +/- P&L coloring) go in **pipes**, not in components.
- Live data (P&L, spot price, signals) arrives via **SignalR**, not polling. `signalr.service.ts` exposes it as observables/signals.
- `environment.ts` holds the API base URL per environment.

---

## 5c. Trading Strategy Specification (Nifty 50 Index Options, Intraday)

> This is the strategy the engine and ML must implement. Risk rules in Section 5d are
> NON-NEGOTIABLE and override any profit target.

### Instrument & scope
- Nifty 50 index options, **intraday only** — never hold overnight (theta decay + gap risk).
- Decisions evaluated continuously (per second / per candle).

### Context inputs (required feature set for every decision)
```
├── DTE (days to expiry)        → theta accelerates near expiry; size down on DTE=0
├── Nifty FUTURES chart          → leads/confirms spot direction
├── India VIX                    → volatility regime; used to scale position size
├── OI + OI change per strike    → support/resistance, directional bias
├── Volume per candle            → conviction filter (NO volume = NO trade)
├── Pre-market bias              → gap up/flat/down + news sentiment
└── Breakout / breakdown levels  → entry triggers
```

### Pre-market reading (before 09:15)
- Estimate open: gap up / flat / gap down (global cues, GIFT Nifty, overnight news).
- Strong/bad news sets the day's directional bias.
- Output: a `premarket_bias` value stored in `market_context`.

### Multi-timeframe analysis (the core edge)
```
HIGHER timeframe (15m / 1h)  → dominant trend & structure
LOWER timeframe (1m / 3m / 5m) → precise entry timing
RULE: only trade when HTF trend and LTF trigger ALIGN.
```

### Regime / no-trade gate (check BEFORE every entry — all must pass)
```
✓ Candle volume > rolling average volume
✓ Clear directional move (not oscillating in a tight band)
✓ Inside a high-liquidity time window
✗ Choppy OR low volume → DO NOT TRADE → log to `no_trades`
```
Sitting out is a measured outcome, not a non-event. Avoiding a loss counts as edge.

### High-liquidity time windows (trade-allowed windows)
```
09:15–09:30   opening volume burst (highest volume, also highest risk)
12:00–14:00   midday momentum
15:00–15:15   closing volume
```
Outside these windows, bias strongly toward no-trade unless structure + volume are exceptional.

### Hold logic — "hold for max profit" defined mechanically
```
IN A WINNING TRADE WITH CLEAR STRUCTURE:
  ✓ TRAIL the stop behind structure (e.g. higher-lows for longs)
  ✓ Let the trend run — do NOT exit on the first small pullback
  ✗ EXIT immediately if: structure breaks / volume dries up / HTF flips / target hit
→ Captures the big move while protecting locked-in profit. "Hold" = trail, not fixed micro-target.
```

### Expiry-day rules (DTE = 0)
```
├── Cheap premiums + violent gamma → biggest wins AND biggest losses
├── Theta brutal after ~13:00 → favor early-session entries
├── Tighter stop losses (moves are faster)
└── REDUCE position size (higher-variance day)
```

### Iterative ML loop
```
Per session / intraday:
  1. Every decision logged (features + outcome: win / loss / no-trade)
  2. Every BAD decision logged to `loss_attribution` with cause + realized loss
  3. End of session → retrain/update model on new labeled data (start: XGBoost nightly)
  4. Track rolling accuracy in `session_reports` → must trend UP over sessions
  5. The loss log is the goldmine — the model learns most from WHY it lost
```

---

## 5d. Risk Guardrails (NON-NEGOTIABLE — override any profit target)

```
├── Risk per trade:        1–2% of capital, HARD cap
├── Daily loss limit:      stop trading for the day at −4% to −5%
├── Position sizing:       scaled by VIX (higher VIX = smaller size)
├── No overnight holds:    flat by close, always
└── AI optimizes for:      risk-adjusted return (Sharpe / consistency), NOT raw return
```

### On the profit target — read this
- The stated ambition is **~50% capital gain in 2 weeks–1 month.** Treat this as a
  **paper-trading HYPOTHESIS to test, not a number to force by over-leveraging.**
- ~50%/month ≈ 600%+ annualized — far beyond what professional desks target as a
  *guaranteed* goal. Chasing it with oversized bets is the fastest way to blow up.
- **In paper trading (Month 1): experiment freely.** The real output is the MEASURED
  numbers — actual win rate, actual avg R:R, actual max drawdown. Let reality calibrate.
- The data then sets the honest target:
  - If paper genuinely shows ~50%/month at acceptable drawdown → rare, but real.
  - If it shows 10–15%/month → still excellent and sustainable.
  - Live result is typically **40–60% of paper** (slippage, spread, API lag, emotion).
- **The edge comes from the regime filter + multi-timeframe discipline, NOT from bet size.**
  Proper sizing compounds a real edge safely. Oversizing to hit a target lets one chop day
  erase a month. The risk caps above are never broken to chase the target.

---

## 6. Python Trading Engine — Structure (Phase 1, build LAST in this round)

> Note: developer is new to Python. Keep it simple, well-commented, type-hinted.
> Do not start this until API + Angular skeletons are reviewed.

```
trading-engine/
├── engine/
│   ├── connectors/          # broker API wrappers (adapters)
│   │   ├── interfaces.py     # IMarketDataProvider, IGreeksProvider, IOrderExecutor
│   │   ├── angel_one.py      # ticks + OHLC + OI  AND  order execution (with 429 backoff)
│   │   ├── upstox.py         # PRIMARY: option chain + Greeks via WebSocket (data only)
│   │   ├── dhan.py           # OPTIONAL backup Greeks (REST) — stub, not built unless needed
│   │   └── kotak_neo.py      # failover stub only
│   ├── collectors/          # write incoming data to DB
│   │   └── tick_collector.py
│   ├── strategy/            # decision logic (maps to Section 5c)
│   │   ├── base_strategy.py
│   │   ├── premarket.py      # gap + news bias (before 09:15)
│   │   ├── regime_gate.py    # chop / low-volume / time-window no-trade gate
│   │   ├── multiframe.py     # HTF trend + LTF trigger alignment
│   │   ├── hold_trail.py     # trailing-stop "hold for max profit" logic
│   │   └── expiry_rules.py   # DTE=0 sizing + tighter stops
│   ├── risk/               # NON-NEGOTIABLE guardrails (Section 5d)
│   │   └── risk_manager.py   # per-trade cap, daily loss limit, VIX sizing
│   ├── ml/                 # iterative learning loop
│   │   ├── features.py       # build feature vector from context inputs
│   │   ├── model.py          # XGBoost train/predict
│   │   └── retrain.py        # end-of-session retrain + accuracy tracking
│   ├── reporting/          # end-of-session report → session_reports table
│   │   └── session_report.py
│   ├── paper/              # simulated order fills + P&L
│   │   └── paper_broker.py
│   ├── storage/            # DB + Redis writers (async, fire-and-forget)
│   │   ├── timescale_writer.py
│   │   └── redis_writer.py
│   ├── models/             # dataclasses / pydantic models
│   └── config.py            # loads secrets from .env
├── tests/
├── .env.example            # API keys placeholders — never commit real keys
├── requirements.txt
└── main.py                  # entrypoint: python main.py
```

- Use **asyncio**; no blocking calls in the tick path.
- DB writes go through an async queue (fire-and-forget) so writing never stalls the strategy.
- Secrets (Angel One keys + TOTP, Upstox token) live in `.env`, loaded via `python-dotenv`. Never hardcode.
- **Provider abstraction:** `angel_one.py` implements `IMarketDataProvider` + `IOrderExecutor`; `upstox.py` implements `IMarketDataProvider`/`IGreeksProvider` ONLY (no order logic). Strategy code depends on the interfaces, never a concrete broker — so swapping the Greeks source (Upstox ↔ Dhan) is a config change, not a rewrite.
- **Order adapter (Angel One) must implement 429 / rate-limit retry with exponential backoff.** Rejected attempts also consume quota, so a burst of rejects can cascade — handle it from the start.
- **Upstox = Plus plan:** budget of 5 WebSocket sockets, expired-contract OHLC available (used in Phase 2 backfill), Depth 30 available if needed. Upstox places ZERO orders.
- **`risk/risk_manager.py` is a hard gate every order passes through** — it can VETO a trade that violates Section 5d caps, regardless of what the strategy or ML says. The risk manager is the final authority.
- Every entry first passes `regime_gate.py`; a blocked entry is logged to `no_trades`. Every loss is written to `loss_attribution` with a cause.

---

## 7. Database Schema (TimescaleDB)

```
option_ticks      — raw per-second snapshot (hypertable on timestamp)
                    cols: time, symbol, strike, option_type, ltp, volume, oi, iv
ohlc_candles      — 1m/5m/15m aggregates
                    cols: time, symbol, interval, open, high, low, close, volume
oi_snapshots      — OI per strike per minute
                    cols: time, symbol, strike, option_type, oi, oi_change
paper_trades      — simulated trades w/ full context
                    cols: id, symbol, side, entry_time, entry_price, exit_time,
                          exit_price, qty, pnl, signal_id, setup_name,
                          htf_trend, ltf_trigger, hold_duration, trail_path_json
strategy_signals  — what triggered each decision + outcome
                    cols: id, time, signal_type, confidence, features_json, outcome
no_trades         — logged decisions to SIT OUT (avoiding a loss = a win)
                    cols: id, time, reason (chop/low_volume/outside_window/dte_risk),
                          context_json
loss_attribution  — WHY a trade lost (the ML goldmine)
                    cols: id, trade_id, cause (wrong_direction/chop/theta/stop_hit/
                          structure_break), realized_loss, context_json
market_context    — per-candle market regime snapshot
                    cols: time, symbol, regime (trending/choppy/breakout/breakdown),
                          volume_vs_avg, vix, dte, htf_trend, premarket_bias
session_reports   — end-of-session summary + ML metrics
                    cols: id, session_date, total_trades, wins, losses, no_trades,
                          gross_pnl, max_drawdown, win_rate, avg_rr, rolling_accuracy,
                          best_setup, notes
```

`option_ticks`, `ohlc_candles`, `oi_snapshots`, `market_context` are **TimescaleDB hypertables** (partitioned on `time`).

---

## 7b. Database Setup & EF Approach

### Software required
- **PostgreSQL** + **TimescaleDB extension** (TimescaleDB is an add-on installed *into* Postgres, not separate software).
- **Redis** — live snapshot cache (used from week 2 onward).
- **DBeaver** (or pgAdmin) — GUI to view/query tables. DBeaver preferred (free, lighter).
- **dotnet-ef** global tool — for migrations: `dotnet tool install --global dotnet-ef`

### Recommended: run the DB stack via Docker
Use the `docker-compose.yml` in the repo root. One command brings up TimescaleDB + Redis:
```bash
docker-compose up -d
```
This avoids manual Postgres/TimescaleDB/Redis installs on Windows and gives a clean reset (`docker-compose down -v`).

Default connection (matches docker-compose):
```
Host=localhost; Port=5432; Database=algotrading; Username=algo; Password=algo_dev_pwd
Redis: localhost:6379
```

### EF approach: CODE-FIRST with Migrations (hybrid for time-series)
- **Yes — code-first using EF Migrations.** Define C# entities → EF generates tables.
- **Hypertable caveat:** EF can `CREATE TABLE` but does NOT know what a TimescaleDB hypertable is.
  So for the time-series tables (`option_ticks`, `ohlc_candles`, `oi_snapshots`), EF creates the
  table normally, then we add **one raw SQL line inside the migration** to convert it:
  ```csharp
  // inside the migration's Up() method, AFTER the CreateTable call:
  migrationBuilder.Sql("SELECT create_hypertable('option_ticks', 'time');");
  ```
  Also enable the extension once in the very first migration:
  ```csharp
  migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS timescaledb;");
  ```
- Normal tables (`paper_trades`, `strategy_signals`) need no special handling — pure code-first.

### Where DB pieces live (clarification)
- **Entities / models / table definitions →** `AlgoTrading.Domain/Entities/` (dependency-free; both Business and Repository reference them).
- **DbContext →** `AlgoTrading.Repository/Context/AppDbContext.cs`
- **EF Fluent configurations →** `AlgoTrading.Repository/Configurations/`
- **Migrations →** `AlgoTrading.Repository/Migrations/` (generated)
- **Repositories →** `AlgoTrading.Repository/Repositories/`

Entities sit in Domain (not Repository) so the Business layer can reference models without depending
on the data-access layer / EF. This keeps the dependency flow clean: `API → Business → Repository → Domain`.

### Migration commands (run from the API project folder = startup project)
```bash
# one-time
dotnet tool install --global dotnet-ef

# create + apply the first migration (DbContext lives in the Repository project)
dotnet ef migrations add InitialCreate --project ../AlgoTrading.Repository --startup-project .
dotnet ef database update --project ../AlgoTrading.Repository --startup-project .
```

### Connection string location
- Lives in `appsettings.json` (non-secret dev) or **user-secrets** for anything sensitive.
- Read via `IConfiguration`. Never hardcode in code.
- Use the **Npgsql** EF provider: `Npgsql.EntityFrameworkCore.PostgreSQL`.

---

## 8. Build Order (do NOT skip ahead)

**Round 0 — DB stack up**
1. `docker-compose up -d` (TimescaleDB + Redis running).
2. Confirm connection in DBeaver.

**Round 1 — API skeleton**
1. Create the 4-project solution + project references.
2. Domain entities (section 7 → C# classes).
3. `AppDbContext` + EF configurations + connection string.
4. `IGenericRepository<T>` + `GenericRepository<T>`.
5. One vertical slice end-to-end: `TradesController → ITradeService → ITradeRepository` returning `PnlSummaryDto`.
6. Swagger working, DI wired via `ServiceCollectionExtensions`.

**Round 2 — Angular skeleton**
1. New Angular app (standalone), set up `app.config.ts`, `app.routes.ts`.
2. `core/` (interceptors, signalr.service stub, api-endpoints config).
3. `shared/models` matching the API DTOs.
4. `dashboard` feature: component + service that calls the API slice from Round 1.
5. One INR pipe + one P&L-color pipe in `shared/pipes`.
6. Lazy-loaded routes verified.

**Round 3 — Python engine** (separate session).

> **Phase 2 (AI training) note:** Because we have Upstox **Plus**, we can pull **expired-contract OHLC** to backfill a historical dataset immediately — no need to wait for live-only collection. Use this to bootstrap the first model training run, then keep enriching with the live data collected in Phase 1.

---

## 9. Conventions

- **.NET:** PascalCase types/methods, `_camelCase` private fields, async suffix `Async`, one class per file, nullable reference types ON.
- **Angular:** kebab-case files, PascalCase classes, suffix by type (`.service.ts`, `.component.ts`, `.pipe.ts`, `.model.ts`).
- **Secrets:** never commit. Use user-secrets (.NET), `.env` (Python), `environment.ts` (Angular, non-secret config only).
- **Git:** small commits per layer. `.gitignore` must exclude `bin/`, `obj/`, `node_modules/`, `.env`, `appsettings.*.json` with secrets.
- Every public method gets a one-line summary comment. Keep functions small.

---

## 10. What NOT To Do

- Do NOT let the API or Angular call into the Python engine.
- Do NOT put business logic in controllers or DB calls in services-via-DbContext directly.
- Do NOT scaffold all phases at once — follow the build order.
- Do NOT hardcode URLs, connection strings, or API keys.
- Do NOT block the tick path in Python with synchronous DB writes.
- Do NOT implement `IOrderExecutor` on the Upstox adapter — Upstox is DATA ONLY. Only Angel One executes orders.
- Do NOT poll Upstox for Greeks — use the WebSocket `option_chain` push stream.
- Do NOT make strategy code depend on a concrete broker class — depend on the provider interfaces only.

---

## 11. Future Scope — Phase 4 (PARKED — DO NOT BUILD YET)

> **GATE: Do not start any of this until the developer explicitly confirms that**
> **(1) intraday is working, (2) it is producing sustainable profit, and**
> **(3) the ML model is giving reliable decisions. Until that confirmation,**
> **this section is reference-only. Do not scaffold, design, or generate code for it.**

**Phase 4 — Positional / Long-Term Equity & Derivatives**

Goal: buy/hold equities & derivatives over week / month / year horizons, and decide the
best day to EXIT existing holdings for maximum profit.

```
Analysis dimensions (both required, weighted):
  ├── FUNDAMENTAL
  │     ├── Earnings, revenue growth, margins, debt
  │     ├── Sector / industry trends
  │     └── News sentiment (results, management, macro)
  └── TECHNICAL
        ├── 52-week high / low positioning
        ├── Higher-timeframe trend (weekly / monthly charts)
        ├── Volume & momentum
        └── Support / resistance for entry & exit timing

Decision output:
  ├── BUY / HOLD / SELL with a horizon tag (week / month / year)
  ├── Best estimated exit window for existing holdings
  └── Conviction score + reasoning logged to DB (same pattern as intraday)

Reuses from Phase 1–3 (this is why intraday is built first):
  ✓ Broker adapters (add equity data endpoints)
  ✓ TimescaleDB (add daily/weekly OHLC + fundamentals tables)
  ✓ .NET API + Angular dashboard (add a "Positions" view)
  ✓ ML loop (different features, same train → log → improve mechanism)
```

Known differences to scope for (when the time comes):
- Fundamentals + news are unstructured and slow-moving — a different ML problem than tick streams.
- Feedback loop is slow (few completed trades/year) → needs heavy historical backfill.
- Will likely need a separate fundamentals data source (evaluate free options then).

---

## 12. Deployment & Automation (BUILD LAST — reference until the app works)

> The developer is a corporate employee and CANNOT be present at market open.
> The engine must start itself before market and run unattended. Build the app first;
> wire scheduling only after the engine runs correctly by hand.

### Where it runs
```
MONTH 1 (paper trading):  old laptop / PC at home, always on. ₹0 cost.
                          Acceptable because no real capital is at risk.
LIVE (Phase 3):           small paid Linux VPS (~₹400/month) for real uptime.
                          A disconnect with real money in a position is unacceptable.
NOTE: GitHub / GitHub Actions CANNOT run the live engine — it is for code + short CI
      jobs only, not a 6-hour live WebSocket process. Do not deploy the engine there.
```

### Scheduler (start engine before market)
```
Linux (VPS / Raspberry Pi) → cron
  # 08:15 IST, Mon–Fri: start engine 1 hour before open
  15 8 * * 1-5   /path/to/start_engine.sh

Windows (home laptop) → Task Scheduler
  Trigger: weekdays 08:15 → Action: run  python main.py
```

### Daily unattended flow
```
08:15  Scheduler starts engine (pre-market phase begins)
08:15–09:15  premarket.py: overnight data, GIFT Nifty, news → premarket_bias
             ML loads trained model
09:15  Market opens → engine trades automatically per strategy (Section 5c)
15:15/15:30  Force-flatten ALL positions (no overnight holds), write session_report
15:30  ML retrains on the day's data (retrain.py) → improves for tomorrow
       Engine sleeps until next scheduled start
DEV:   checks Angular dashboard from phone/office anytime (reads DB only — separate)
```

### ML training data
```
INITIAL:  backfill expired-contract option OHLC from Upstox Plus → train first model
          before month-1 collection even finishes.
ONGOING:  each day post-close, retrain on (today's collected data) + (all prior).
          Runs as part of the 15:30 routine. No manual step.
```

---

## 13. Unattended Safety Checklist (MUST be bulletproof before ANY live capital)

> Running automated while away means NO human watching when something breaks.
> These are mandatory before Phase 3. During paper trading, still implement + test them.

```
□ Daily loss limit auto-halt   → at −4% to −5% for the day, engine STOPS trading,
                                  flattens, and does not re-enter until next day.
□ Per-trade risk cap enforced  → risk_manager.py VETOES any order over 1–2% risk.
□ Auto square-off              → all positions force-closed before market close, always.
□ Max open positions cap       → hard limit on concurrent positions.
□ Connection-loss handling     → if a broker WebSocket drops mid-session: attempt
                                  reconnect; if in a position and cannot recover,
                                  flatten via the order API and alert.
□ Kill switch                  → a manual remote way to halt the engine (e.g. a flag
                                  in Redis the engine checks each loop) so the dev can
                                  stop it from their phone if needed.
□ Alerting                     → on halt / error / connection loss, send a notification
                                  (Telegram bot / email) so the dev knows immediately.
□ Heartbeat                    → engine writes a "last alive" timestamp to Redis every
                                  few seconds; dashboard shows LIVE / OFFLINE from it.
□ Startup self-check           → on launch, verify broker auth, DB, Redis all reachable
                                  BEFORE market open; alert if any fail.
```
The risk caps in Section 5d are the final authority — an unattended system makes them
more important, not less. No target ever justifies disabling them.
