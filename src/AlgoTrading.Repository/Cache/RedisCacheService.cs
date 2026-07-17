using System.Text.Json;
using StackExchange.Redis;

namespace AlgoTrading.Repository.Cache;

/// <summary>StackExchange.Redis-backed read-only snapshot reader.</summary>
public class RedisCacheService : IRedisCacheService
{
    private const string HeartbeatKey = "engine:heartbeat";
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis) => _redis = redis;

    private IDatabase Db => _redis.GetDatabase();

    public async Task<string?> GetStringAsync(string key)
    {
        var value = await Db.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await Db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task<DateTimeOffset?> GetHeartbeatAsync()
    {
        var value = await Db.StringGetAsync(HeartbeatKey);
        if (value.IsNullOrEmpty) return null;
        // Engine writes a Unix-ms epoch as the heartbeat.
        return long.TryParse(value, out var ms)
            ? DateTimeOffset.FromUnixTimeMilliseconds(ms)
            : null;
    }
}
