using AlgoTrading.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlgoTrading.Repository.Configurations;

/// <summary>EF mapping for the market_context hypertable.</summary>
public class MarketContextConfiguration : IEntityTypeConfiguration<MarketContext>
{
    public void Configure(EntityTypeBuilder<MarketContext> b)
    {
        b.ToTable("market_context");
        b.HasKey(x => new { x.Time, x.Symbol });
        b.Property(x => x.Symbol).HasMaxLength(30);
        b.Property(x => x.Regime).HasMaxLength(20);
        b.Property(x => x.HtfTrend).HasMaxLength(20);
        b.Property(x => x.PremarketBias).HasMaxLength(20);
    }
}
