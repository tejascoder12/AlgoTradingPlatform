namespace AlgoTrading.Repository.Cache;

/// <summary>Reads the live snapshot the Python engine writes to Redis (read-only path).</summary>
public interface IRedisCacheService
{
    /// <summary>Get a raw string value by key, or null if absent.</summary>
    Task<string?> GetStringAsync(string key);

    /// <summary>Get and deserialize a JSON value by key, or default if absent.</summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>Engine heartbeat timestamp (Section 13) — used to show LIVE/OFFLINE.</summary>
    Task<DateTimeOffset?> GetHeartbeatAsync();
}
