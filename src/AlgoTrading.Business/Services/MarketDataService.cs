using AlgoTrading.Business.DTOs;
using AlgoTrading.Business.Interfaces;
using AlgoTrading.Business.Mappers;
using AlgoTrading.Repository.Cache;
using AlgoTrading.Repository.Interfaces;

namespace AlgoTrading.Business.Services;

/// <summary>Read-only market data + engine-status logic (DB + Redis).</summary>
public class MarketDataService : IMarketDataService
{
    // The engine is considered offline if no heartbeat within this window (Section 13).
    private const double OfflineThresholdSeconds = 15;

    // Fallback window, used when there is no Redis heartbeat. Wider because it
    // measures tick writes (one per engine poll, ~5s) rather than a dedicated
    // heartbeat, and must tolerate a few retried data-provider fetches.
    private const double DbActivityThresholdSeconds = 60;

    private readonly IMarketDataRepository _marketData;
    private readonly IRedisCacheService _cache;

    public MarketDataService(IMarketDataRepository marketData, IRedisCacheService cache)
    {
        _marketData = marketData;
        _cache = cache;
    }

    public async Task<IReadOnlyList<OhlcCandleDto>> GetRecentCandlesAsync(string symbol, string interval, int limit, CancellationToken ct = default)
    {
        var candles = await _marketData.GetRecentCandlesAsync(symbol, interval, limit, ct);
        // Return chronological order for charting.
        return candles.Reverse().Select(c => c.ToDto()).ToList();
    }

    public async Task<EngineStatusDto> GetEngineStatusAsync(CancellationToken ct = default)
    {
        // Preferred signal: the Redis heartbeat (§13). Authoritative whenever present.
        var heartbeat = await _cache.GetHeartbeatAsync();
        if (heartbeat is not null)
        {
            var hbAge = (DateTimeOffset.UtcNow - heartbeat.Value).TotalSeconds;
            return new EngineStatusDto
            {
                IsOnline = hbAge <= OfflineThresholdSeconds,
                LastHeartbeat = heartbeat,
                SecondsSinceHeartbeat = hbAge,
                Source = "redis"
            };
        }

        // The engine does not publish a heartbeat yet, so infer liveness from what
        // it already writes: an open run row plus recent tick writes. Requiring BOTH
        // means a run that died without closing its row reads OFFLINE (its ticks go
        // stale) instead of showing a false LIVE forever.
        var session = await _marketData.GetActiveEngineSessionAsync(ct);
        if (session is null)
            return new EngineStatusDto { IsOnline = false, Source = "database" };

        var lastTick = await _marketData.GetLastTickTimeAsync(ct);
        // A run that just started has not written a tick yet — count its start as activity.
        var lastActivity = lastTick > session.StartedAt ? lastTick!.Value : session.StartedAt;
        var age = (DateTimeOffset.UtcNow - lastActivity).TotalSeconds;

        return new EngineStatusDto
        {
            IsOnline = age <= DbActivityThresholdSeconds,
            LastHeartbeat = lastActivity,
            SecondsSinceHeartbeat = age,
            Source = "database"
        };
    }
}
