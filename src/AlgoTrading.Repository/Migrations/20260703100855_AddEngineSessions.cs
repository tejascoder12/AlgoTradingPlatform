using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AlgoTrading.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddEngineSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "engine_sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: true),
                    Mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    StopReason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TotalTrades = table.Column<int>(type: "integer", nullable: false),
                    GrossPnl = table.Column<decimal>(type: "numeric(14,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engine_sessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_engine_sessions_SessionDate",
                table: "engine_sessions",
                column: "SessionDate");

            migrationBuilder.CreateIndex(
                name: "IX_engine_sessions_StartedAt",
                table: "engine_sessions",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "engine_sessions");
        }
    }
}
