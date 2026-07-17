using AlgoTrading.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlgoTrading.Repository.Configurations;

/// <summary>EF mapping for paper_trades (normal table, surrogate PK).</summary>
public class PaperTradeConfiguration : IEntityTypeConfiguration<PaperTrade>
{
    public void Configure(EntityTypeBuilder<PaperTrade> b)
    {
        b.ToTable("paper_trades");
        b.HasKey(x => x.Id);
        b.Property(x => x.Symbol).HasMaxLength(30);
        b.Property(x => x.Side).HasConversion<string>().HasMaxLength(4);
        b.Property(x => x.EntryPrice).HasColumnType("numeric(12,2)");
        b.Property(x => x.ExitPrice).HasColumnType("numeric(12,2)");
        b.Property(x => x.Pnl).HasColumnType("numeric(14,2)");
        b.Property(x => x.SetupName).HasMaxLength(60);
        b.Property(x => x.HtfTrend).HasMaxLength(20);
        b.Property(x => x.LtfTrigger).HasMaxLength(20);

        b.HasOne(x => x.Signal)
            .WithMany()
            .HasForeignKey(x => x.SignalId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.EntryTime);
    }
}
