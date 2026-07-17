using AlgoTrading.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlgoTrading.Repository.Configurations;

/// <summary>EF mapping for no_trades (normal table).</summary>
public class NoTradeConfiguration : IEntityTypeConfiguration<NoTrade>
{
    public void Configure(EntityTypeBuilder<NoTrade> b)
    {
        b.ToTable("no_trades");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(30);
        b.Property(x => x.ContextJson).HasColumnType("jsonb");
        b.HasIndex(x => x.Time);
    }
}
