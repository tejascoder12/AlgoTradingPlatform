using AlgoTrading.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlgoTrading.Repository.Configurations;

/// <summary>EF mapping for loss_attribution (normal table).</summary>
public class LossAttributionConfiguration : IEntityTypeConfiguration<LossAttribution>
{
    public void Configure(EntityTypeBuilder<LossAttribution> b)
    {
        b.ToTable("loss_attribution");
        b.HasKey(x => x.Id);
        b.Property(x => x.Cause).HasMaxLength(30);
        b.Property(x => x.RealizedLoss).HasColumnType("numeric(14,2)");
        b.Property(x => x.ContextJson).HasColumnType("jsonb");

        b.HasOne(x => x.Trade)
            .WithMany()
            .HasForeignKey(x => x.TradeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
