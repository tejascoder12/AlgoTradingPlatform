using AlgoTrading.Domain.Enums;

namespace AlgoTrading.Domain.Entities;

/// <summary>Open-interest per strike per minute (TimescaleDB hypertable on Time).</summary>
public class OiSnapshot
{
    public DateTimeOffset Time { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int Strike { get; set; }
    public OptionType OptionType { get; set; }
    public long Oi { get; set; }
    public long OiChange { get; set; }
}
