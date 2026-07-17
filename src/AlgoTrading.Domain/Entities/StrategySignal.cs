using AlgoTrading.Domain.Enums;

namespace AlgoTrading.Domain.Entities;

/// <summary>What triggered a decision (features + outcome) — the ML training record.</summary>
public class StrategySignal
{
    public int Id { get; set; }
    public DateTimeOffset Time { get; set; }
    public SignalType SignalType { get; set; }
    /// <summary>Model/strategy confidence, 0..1.</summary>
    public double Confidence { get; set; }
    /// <summary>Full feature vector used for the decision, serialized as JSON.</summary>
    public string? FeaturesJson { get; set; }
    /// <summary>Realized outcome: win / loss / no-trade (filled after the fact).</summary>
    public string? Outcome { get; set; }
}
