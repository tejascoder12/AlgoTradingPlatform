using AlgoTrading.Domain.Entities;

namespace AlgoTrading.Repository.Interfaces;

/// <summary>Read access to time-series market data written by the Python engine.</summary>
public interface IMarketDataRepository
{
    /// <summary>Latest OHLC candles for a symbol/interval, newest first.</summary>
    Task<IReadOnlyList<OhlcCandle>> GetRecentCandlesAsync(string symbol, string interval, int limit, CancellationToken ct = default);

    /// <summary>Latest per-second option ticks for a symbol, newest first.</summary>
    Task<IReadOnlyList<OptionTick>> GetRecentTicksAsync(string symbol, int limit, CancellationToken ct = default);

    /// <summary>Timestamp of the most recent tick written by the engine; null if none.</summary>
    Task<DateTimeOffset?> GetLastTickTimeAsync(CancellationToken ct = default);

    /// <summary>The engine run that is still open (no EndedAt); null when no run is active.</summary>
    Task<EngineSession?> GetActiveEngineSessionAsync(CancellationToken ct = default);
}
