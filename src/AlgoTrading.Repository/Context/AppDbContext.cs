using AlgoTrading.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlgoTrading.Repository.Context;

/// <summary>EF Core DbContext for the AlgoTrading TimescaleDB/Postgres database.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<OptionTick> OptionTicks => Set<OptionTick>();
    public DbSet<OhlcCandle> OhlcCandles => Set<OhlcCandle>();
    public DbSet<OiSnapshot> OiSnapshots => Set<OiSnapshot>();
    public DbSet<PaperTrade> PaperTrades => Set<PaperTrade>();
    public DbSet<StrategySignal> StrategySignals => Set<StrategySignal>();
    public DbSet<NoTrade> NoTrades => Set<NoTrade>();
    public DbSet<LossAttribution> LossAttributions => Set<LossAttribution>();
    public DbSet<MarketContext> MarketContexts => Set<MarketContext>();
    public DbSet<SessionReport> SessionReports => Set<SessionReport>();
    public DbSet<EngineSession> EngineSessions => Set<EngineSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply every IEntityTypeConfiguration in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
