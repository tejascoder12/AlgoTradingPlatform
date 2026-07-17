namespace AlgoTrading.Business.DTOs;

/// <summary>Live engine status derived from the Redis heartbeat (Section 13).</summary>
public record EngineStatusDto
{
    public bool IsOnline { get; init; }
    public DateTimeOffset? LastHeartbeat { get; init; }
    public double? SecondsSinceHeartbeat { get; init; }
    /// <summary>Where the status came from: "redis" (heartbeat) or "database" (run row + tick activity).</summary>
    public string? Source { get; init; }
}
