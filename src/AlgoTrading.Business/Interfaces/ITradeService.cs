using AlgoTrading.Business.DTOs;

namespace AlgoTrading.Business.Interfaces;

/// <summary>Business operations over paper trades and P&amp;L.</summary>
public interface ITradeService
{
    Task<IReadOnlyList<TradeDto>> GetTradesForDateAsync(DateOnly date, CancellationToken ct = default);
    Task<TradeDto?> GetTradeByIdAsync(int id, CancellationToken ct = default);
    Task<PnlSummaryDto> GetPnlSummaryAsync(CancellationToken ct = default);

    /// <summary>Summary of today's (IST) session only — P&amp;L, win rate, best/worst trade.</summary>
    Task<TodaySummaryDto> GetTodaySummaryAsync(CancellationToken ct = default);
}
