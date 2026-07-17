namespace AlgoTrading.Business.DTOs;

/// <summary>OHLC candle shaped for the charting library on the frontend.</summary>
public record OhlcCandleDto
{
    public DateTimeOffset Time { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string Interval { get; init; } = string.Empty;
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public long Volume { get; init; }
}
