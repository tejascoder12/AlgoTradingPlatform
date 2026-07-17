using AlgoTrading.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlgoTrading.Repository.Configurations;

/// <summary>EF mapping for the oi_snapshots hypertable.</summary>
public class OiSnapshotConfiguration : IEntityTypeConfiguration<OiSnapshot>
{
    public void Configure(EntityTypeBuilder<OiSnapshot> b)
    {
        b.ToTable("oi_snapshots");
        b.HasKey(x => new { x.Time, x.Symbol, x.Strike, x.OptionType });
        b.Property(x => x.Symbol).HasMaxLength(30);
        b.Property(x => x.OptionType).HasConversion<string>().HasMaxLength(2);
        b.HasIndex(x => new { x.Symbol, x.Time });
    }
}
