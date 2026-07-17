using AlgoTrading.Business.DTOs;
using AlgoTrading.Business.Interfaces;
using AlgoTrading.Business.Mappers;
using AlgoTrading.Repository.Interfaces;

namespace AlgoTrading.Business.Services;

/// <summary>Trade + P&amp;L business logic. Depends on repository interfaces only.</summary>
public class TradeService : ITradeService
{
    // IST has no DST, so a fixed offset is exact and avoids OS timezone-id differences.
    private static readonly TimeSpan IstOffset = TimeSpan.FromHours(5.5);

    private readonly ITradeRepository _trades;

    public TradeService(ITradeRepository trades) => _trades = trades;

    public async Task<IReadOnlyList<TradeDto>> GetTradesForDateAsync(DateOnly date, CancellationToken ct = default)
    {
        var trades = await _trades.GetByDateAsync(date, ct);
        return trades.Select(t => t.ToDto()).ToList();
    }

    public async Task<TradeDto?> GetTradeByIdAsync(int id, CancellationToken ct = default)
    {
        var trade = await _trades.GetByIdAsync(id, ct);
        return trade?.ToDto();
    }

    public async Task<PnlSummaryDto> GetPnlSummaryAsync(CancellationToken ct = default)
    {
        var all = await _trades.GetAllAsync(ct);
        var closed = all.Where(t => t.ExitTime != null && t.Pnl != null).ToList();

        var wins = closed.Count(t => t.Pnl > 0);
        var losses = closed.Count(t => t.Pnl < 0);
        var realized = closed.Sum(t => t.Pnl ?? 0m);

        return new PnlSummaryDto
        {
            TotalTrades = all.Count,
            Wins = wins,
            Losses = losses,
            OpenPositions = all.Count(t => t.ExitTime is null),
            GrossPnl = all.Sum(t => t.Pnl ?? 0m),
            RealizedPnl = realized,
            WinRate = closed.Count > 0 ? (double)wins / closed.Count : 0d,
            BestTradePnl = closed.Count > 0 ? closed.Max(t => t.Pnl ?? 0m) : 0m,
            WorstTradePnl = closed.Count > 0 ? closed.Min(t => t.Pnl ?? 0m) : 0m
        };
    }

    public async Task<TodaySummaryDto> GetTodaySummaryAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(IstOffset).DateTime);
        var trades = await _trades.GetByDateAsync(today, ct);
        var closed = trades.Where(t => t.ExitTime != null && t.Pnl != null).ToList();

        var wins = closed.Count(t => t.Pnl > 0);
        var best = closed.OrderByDescending(t => t.Pnl).FirstOrDefault();
        var worst = closed.OrderBy(t => t.Pnl).FirstOrDefault();

        return new TodaySummaryDto
        {
            SessionDate = today,
            TotalTrades = trades.Count,
            Wins = wins,
            Losses = closed.Count(t => t.Pnl < 0),
            OpenPositions = trades.Count(t => t.ExitTime is null),
            GrossPnl = trades.Sum(t => t.Pnl ?? 0m),
            RealizedPnl = closed.Sum(t => t.Pnl ?? 0m),
            WinRate = closed.Count > 0 ? (double)wins / closed.Count : 0d,
            BestTradePnl = best?.Pnl ?? 0m,
            WorstTradePnl = worst?.Pnl ?? 0m,
            BestTradeSymbol = best?.Symbol,
            WorstTradeSymbol = worst?.Symbol,
            HasTraded = trades.Count > 0
        };
    }
}
