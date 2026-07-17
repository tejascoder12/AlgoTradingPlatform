using AlgoTrading.Business.DTOs;

namespace AlgoTrading.Business.Interfaces;

/// <summary>Read access to time-series market data + live engine status.</summary>
public interface IMarketDataService
{
    Task<IReadOnlyList<OhlcCandleDto>> GetRecentCandlesAsync(string symbol, string interval, int limit, CancellationToken ct = default);
    Task<EngineStatusDto> GetEngineStatusAsync(CancellationToken ct = default);
}
