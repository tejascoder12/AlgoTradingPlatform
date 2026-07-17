using AlgoTrading.Domain.Entities;

namespace AlgoTrading.Repository.Interfaces;

/// <summary>Custom queries over paper trades beyond basic CRUD.</summary>
public interface ITradeRepository : IGenericRepository<PaperTrade>
{
    /// <summary>Trades entered on a given date, most recent first.</summary>
    Task<IReadOnlyList<PaperTrade>> GetByDateAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>All closed trades (ExitTime set), used for P&L aggregation.</summary>
    Task<IReadOnlyList<PaperTrade>> GetClosedAsync(CancellationToken ct = default);
}
