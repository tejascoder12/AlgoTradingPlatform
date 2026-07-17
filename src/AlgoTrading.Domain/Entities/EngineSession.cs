namespace AlgoTrading.Domain.Entities;

/// <summary>
/// One row per application RUN of the trading engine (not per day).
/// Enables "how many times ran today" (count by date) and "total hours run"
/// (sum of DurationSeconds). Distinct from SessionReport, which is the daily rollup.
/// </summary>
public class EngineSession
{
    public int Id { get; set; }
    public DateOnly SessionDate { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    /// <summary>Null while the run is still active; set at square-off / shutdown.</summary>
    public DateTimeOffset? EndedAt { get; set; }
    public double? DurationSeconds { get; set; }

    /// <summary>Run mode: "live" | "test" | "demo".</summary>
    public string Mode { get; set; } = string.Empty;
    /// <summary>Why the run ended: "square_off" | "manual" | "test_timeout" | "error".</summary>
    public string? StopReason { get; set; }

    public int TotalTrades { get; set; }
    public decimal GrossPnl { get; set; }
}
