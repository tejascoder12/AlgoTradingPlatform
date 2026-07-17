using AlgoTrading.Domain.Enums;

namespace AlgoTrading.Domain.Entities;

/// <summary>A simulated (paper) trade with the full decision context that produced it.</summary>
public class PaperTrade
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public TradeSide Side { get; set; }
    public DateTimeOffset EntryTime { get; set; }
    public decimal EntryPrice { get; set; }
    public DateTimeOffset? ExitTime { get; set; }
    public decimal? ExitPrice { get; set; }
    public int Qty { get; set; }
    public decimal? Pnl { get; set; }

    /// <summary>FK to the signal that triggered this trade.</summary>
    public int? SignalId { get; set; }
    public StrategySignal? Signal { get; set; }

    public string? SetupName { get; set; }
    public string? HtfTrend { get; set; }
    public string? LtfTrigger { get; set; }
    /// <summary>Total time the position was held.</summary>
    public TimeSpan? HoldDuration { get; set; }
    /// <summary>JSON path of trailing-stop movements over the life of the trade.</summary>
    public string? TrailPathJson { get; set; }
}
