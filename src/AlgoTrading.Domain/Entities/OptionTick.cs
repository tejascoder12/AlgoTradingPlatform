using AlgoTrading.Domain.Enums;

namespace AlgoTrading.Domain.Entities;

/// <summary>Raw per-second snapshot of a single option contract (TimescaleDB hypertable on Time).</summary>
public class OptionTick
{
    public DateTimeOffset Time { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int Strike { get; set; }
    public OptionType OptionType { get; set; }
    public decimal Ltp { get; set; }
    public long Volume { get; set; }
    public long Oi { get; set; }
    public decimal Iv { get; set; }
}
