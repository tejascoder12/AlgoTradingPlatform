using AlgoTrading.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlgoTrading.Repository.Configurations;

/// <summary>EF mapping for the ohlc_candles hypertable.</summary>
public class OhlcCandleConfiguration : IEntityTypeConfiguration<OhlcCandle>
{
    public void Configure(EntityTypeBuilder<OhlcCandle> b)
    {
        b.ToTable("ohlc_candles");
        b.HasKey(x => new { x.Time, x.Symbol, x.Interval });
        b.Property(x => x.Symbol).HasMaxLength(30);
        b.Property(x => x.Interval).HasMaxLength(5);
        b.Property(x => x.Open).HasColumnType("numeric(12,2)");
        b.Property(x => x.High).HasColumnType("numeric(12,2)");
        b.Property(x => x.Low).HasColumnType("numeric(12,2)");
        b.Property(x => x.Close).HasColumnType("numeric(12,2)");
        b.HasIndex(x => new { x.Symbol, x.Interval, x.Time });
    }
}
