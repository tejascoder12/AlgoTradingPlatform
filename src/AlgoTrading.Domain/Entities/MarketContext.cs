namespace AlgoTrading.Domain.Entities;

/// <summary>Per-candle market regime snapshot (TimescaleDB hypertable on Time).</summary>
public class MarketContext
{
    public DateTimeOffset Time { get; set; }
    public string Symbol { get; set; } = string.Empty;
    /// <summary>Regime: trending / choppy / breakout / breakdown.</summary>
    public string Regime { get; set; } = string.Empty;
    /// <summary>Current candle volume vs rolling average (ratio).</summary>
    public double VolumeVsAvg { get; set; }
    public double Vix { get; set; }
    /// <summary>Days to expiry.</summary>
    public int Dte { get; set; }
    public string? HtfTrend { get; set; }
    /// <summary>Pre-market bias: gap_up / flat / gap_down.</summary>
    public string? PremarketBias { get; set; }
}
