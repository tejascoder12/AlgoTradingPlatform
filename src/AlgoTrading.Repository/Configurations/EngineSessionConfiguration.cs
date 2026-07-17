using AlgoTrading.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlgoTrading.Repository.Configurations;

/// <summary>EF mapping for engine_sessions (normal table, one row per run).</summary>
public class EngineSessionConfiguration : IEntityTypeConfiguration<EngineSession>
{
    public void Configure(EntityTypeBuilder<EngineSession> b)
    {
        b.ToTable("engine_sessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Mode).HasMaxLength(10);
        b.Property(x => x.StopReason).HasMaxLength(20);
        b.Property(x => x.GrossPnl).HasColumnType("numeric(14,2)");
        b.HasIndex(x => x.SessionDate);
        b.HasIndex(x => x.StartedAt);
    }
}
