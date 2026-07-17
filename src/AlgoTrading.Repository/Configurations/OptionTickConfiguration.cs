using AlgoTrading.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlgoTrading.Repository.Configurations;

/// <summary>EF mapping for the option_ticks hypertable.</summary>
public class OptionTickConfiguration : IEntityTypeConfiguration<OptionTick>
{
    public void Configure(EntityTypeBuilder<OptionTick> b)
    {
        b.ToTable("option_ticks");
        // Hypertables have no surrogate PK; a composite key uniquely identifies a tick.
        b.HasKey(x => new { x.Time, x.Symbol, x.Strike, x.OptionType });
        b.Property(x => x.Symbol).HasMaxLength(30);
        b.Property(x => x.OptionType).HasConversion<string>().HasMaxLength(2);
        b.Property(x => x.Ltp).HasColumnType("numeric(12,2)");
        b.Property(x => x.Iv).HasColumnType("numeric(8,4)");
        b.HasIndex(x => new { x.Symbol, x.Time });
    }
}
