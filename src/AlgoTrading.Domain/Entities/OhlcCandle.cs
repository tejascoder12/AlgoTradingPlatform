namespace AlgoTrading.Domain.Entities;

/// <summary>1m/5m/15m OHLC aggregate candle (TimescaleDB hypertable on Time).</summary>
public class OhlcCandle
{
    public DateTimeOffset Time { get; set; }
    public string Symbol { get; set; } = string.Empty;
    /// <summary>Candle interval, e.g. "1m", "5m", "15m".</summary>
    public string Interval { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}
