using AlgoTrading.Domain.Entities;
using AlgoTrading.Repository.Context;
using AlgoTrading.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlgoTrading.Repository.Repositories;

/// <summary>Paper-trade data access.</summary>
public class TradeRepository : GenericRepository<PaperTrade>, ITradeRepository
{
    // IST has no DST, so a fixed offset is exact and avoids OS timezone-id differences.
    private static readonly TimeSpan IstOffset = TimeSpan.FromHours(5.5);

    public TradeRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<PaperTrade>> GetByDateAsync(DateOnly date, CancellationToken ct = default)
    {
        // `date` is an IST trading date — the market and the engine both run in IST.
        // Npgsql only binds UTC-offset values to timestamptz, so send the same
        // instants as UTC.
        var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), IstOffset).ToUniversalTime();
        var end = start.AddDays(1);
        return await Set.AsNoTracking()
            .Where(t => t.EntryTime >= start && t.EntryTime < end)
            .OrderByDescending(t => t.EntryTime)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PaperTrade>> GetClosedAsync(CancellationToken ct = default)
        => await Set.AsNoTracking()
            .Where(t => t.ExitTime != null)
            .OrderByDescending(t => t.ExitTime)
            .ToListAsync(ct);
}
