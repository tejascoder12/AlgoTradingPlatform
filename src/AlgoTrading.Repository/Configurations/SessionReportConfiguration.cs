using AlgoTrading.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlgoTrading.Repository.Configurations;

/// <summary>EF mapping for session_reports (normal table).</summary>
public class SessionReportConfiguration : IEntityTypeConfiguration<SessionReport>
{
    public void Configure(EntityTypeBuilder<SessionReport> b)
    {
        b.ToTable("session_reports");
        b.HasKey(x => x.Id);
        b.Property(x => x.GrossPnl).HasColumnType("numeric(14,2)");
        b.Property(x => x.MaxDrawdown).HasColumnType("numeric(14,2)");
        b.Property(x => x.BestSetup).HasMaxLength(60);
        b.HasIndex(x => x.SessionDate).IsUnique();
    }
}
