using AlgoTrading.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlgoTrading.Repository.Configurations;

/// <summary>EF mapping for strategy_signals (normal table).</summary>
public class StrategySignalConfiguration : IEntityTypeConfiguration<StrategySignal>
{
    public void Configure(EntityTypeBuilder<StrategySignal> b)
    {
        b.ToTable("strategy_signals");
        b.HasKey(x => x.Id);
        b.Property(x => x.SignalType).HasConversion<string>().HasMaxLength(6);
        b.Property(x => x.FeaturesJson).HasColumnType("jsonb");
        b.Property(x => x.Outcome).HasMaxLength(20);
        b.HasIndex(x => x.Time);
    }
}
