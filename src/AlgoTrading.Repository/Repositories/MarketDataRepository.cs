using AlgoTrading.Domain.Entities;
using AlgoTrading.Repository.Context;
using AlgoTrading.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlgoTrading.Repository.Repositories;

/// <summary>Read-only access to the time-series market data tables.</summary>
public class MarketDataRepository : IMarketDataRepository
{
    private readonly AppDbContext _context;

    public MarketDataRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<OhlcCandle>> GetRecentCandlesAsync(string symbol, string interval, int limit, CancellationToken ct = default)
        => await _context.OhlcCandles.AsNoTracking()
            .Where(c => c.Symbol == symbol && c.Interval == interval)
            .OrderByDescending(c => c.Time)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<OptionTick>> GetRecentTicksAsync(string symbol, int limit, CancellationToken ct = default)
        => await _context.OptionTicks.AsNoTracking()
            .Where(t => t.Symbol == symbol)
            .OrderByDescending(t => t.Time)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<DateTimeOffset?> GetLastTickTimeAsync(CancellationToken ct = default)
        => await _context.OptionTicks.AsNoTracking()
            .MaxAsync(t => (DateTimeOffset?)t.Time, ct);

    public async Task<EngineSession?> GetActiveEngineSessionAsync(CancellationToken ct = default)
        => await _context.EngineSessions.AsNoTracking()
            .Where(s => s.EndedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(ct);
}
