using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AlgoTrading.Repository.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable the TimescaleDB extension once (CLAUDE.md §7b).
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS timescaledb;");

            migrationBuilder.CreateTable(
                name: "market_context",
                columns: table => new
                {
                    Time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Regime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VolumeVsAvg = table.Column<double>(type: "double precision", nullable: false),
                    Vix = table.Column<double>(type: "double precision", nullable: false),
                    Dte = table.Column<int>(type: "integer", nullable: false),
                    HtfTrend = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PremarketBias = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_context", x => new { x.Time, x.Symbol });
                });

            migrationBuilder.CreateTable(
                name: "no_trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ContextJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_no_trades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ohlc_candles",
                columns: table => new
                {
                    Time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Interval = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Open = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    High = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Low = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Close = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ohlc_candles", x => new { x.Time, x.Symbol, x.Interval });
                });

            migrationBuilder.CreateTable(
                name: "oi_snapshots",
                columns: table => new
                {
                    Time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Strike = table.Column<int>(type: "integer", nullable: false),
                    OptionType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Oi = table.Column<long>(type: "bigint", nullable: false),
                    OiChange = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_snapshots", x => new { x.Time, x.Symbol, x.Strike, x.OptionType });
                });

            migrationBuilder.CreateTable(
                name: "option_ticks",
                columns: table => new
                {
                    Time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Strike = table.Column<int>(type: "integer", nullable: false),
                    OptionType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Ltp = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    Oi = table.Column<long>(type: "bigint", nullable: false),
                    Iv = table.Column<decimal>(type: "numeric(8,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_option_ticks", x => new { x.Time, x.Symbol, x.Strike, x.OptionType });
                });

            migrationBuilder.CreateTable(
                name: "session_reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalTrades = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    NoTrades = table.Column<int>(type: "integer", nullable: false),
                    GrossPnl = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    MaxDrawdown = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    WinRate = table.Column<double>(type: "double precision", nullable: false),
                    AvgRr = table.Column<double>(type: "double precision", nullable: false),
                    RollingAccuracy = table.Column<double>(type: "double precision", nullable: false),
                    BestSetup = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "strategy_signals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SignalType = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    FeaturesJson = table.Column<string>(type: "jsonb", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategy_signals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "paper_trades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Side = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    EntryTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ExitTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExitPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    Qty = table.Column<int>(type: "integer", nullable: false),
                    Pnl = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    SignalId = table.Column<int>(type: "integer", nullable: true),
                    SetupName = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    HtfTrend = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LtfTrigger = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    HoldDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    TrailPathJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_trades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paper_trades_strategy_signals_SignalId",
                        column: x => x.SignalId,
                        principalTable: "strategy_signals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "loss_attribution",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TradeId = table.Column<int>(type: "integer", nullable: false),
                    Cause = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RealizedLoss = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ContextJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loss_attribution", x => x.Id);
                    table.ForeignKey(
                        name: "FK_loss_attribution_paper_trades_TradeId",
                        column: x => x.TradeId,
                        principalTable: "paper_trades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_loss_attribution_TradeId",
                table: "loss_attribution",
                column: "TradeId");

            migrationBuilder.CreateIndex(
                name: "IX_no_trades_Time",
                table: "no_trades",
                column: "Time");

            migrationBuilder.CreateIndex(
                name: "IX_ohlc_candles_Symbol_Interval_Time",
                table: "ohlc_candles",
                columns: new[] { "Symbol", "Interval", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_oi_snapshots_Symbol_Time",
                table: "oi_snapshots",
                columns: new[] { "Symbol", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_option_ticks_Symbol_Time",
                table: "option_ticks",
                columns: new[] { "Symbol", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_paper_trades_EntryTime",
                table: "paper_trades",
                column: "EntryTime");

            migrationBuilder.CreateIndex(
                name: "IX_paper_trades_SignalId",
                table: "paper_trades",
                column: "SignalId");

            migrationBuilder.CreateIndex(
                name: "IX_session_reports_SessionDate",
                table: "session_reports",
                column: "SessionDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_strategy_signals_Time",
                table: "strategy_signals",
                column: "Time");

            // Convert the time-series tables to TimescaleDB hypertables (CLAUDE.md §7b).
            // migrate_data => true lets conversion succeed even if rows already exist.
            migrationBuilder.Sql("SELECT create_hypertable('option_ticks', 'Time', migrate_data => true);");
            migrationBuilder.Sql("SELECT create_hypertable('ohlc_candles', 'Time', migrate_data => true);");
            migrationBuilder.Sql("SELECT create_hypertable('oi_snapshots', 'Time', migrate_data => true);");
            migrationBuilder.Sql("SELECT create_hypertable('market_context', 'Time', migrate_data => true);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loss_attribution");

            migrationBuilder.DropTable(
                name: "market_context");

            migrationBuilder.DropTable(
                name: "no_trades");

            migrationBuilder.DropTable(
                name: "ohlc_candles");

            migrationBuilder.DropTable(
                name: "oi_snapshots");

            migrationBuilder.DropTable(
                name: "option_ticks");

            migrationBuilder.DropTable(
                name: "session_reports");

            migrationBuilder.DropTable(
                name: "paper_trades");

            migrationBuilder.DropTable(
                name: "strategy_signals");
        }
    }
}
