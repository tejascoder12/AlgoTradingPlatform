namespace AlgoTrading.Business.DTOs;

/// <summary>API-facing view of a paper trade (no EF navigation properties).</summary>
public record TradeDto
{
    public int Id { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string Side { get; init; } = string.Empty;
    public DateTimeOffset EntryTime { get; init; }
    public decimal EntryPrice { get; init; }
    public DateTimeOffset? ExitTime { get; init; }
    public decimal? ExitPrice { get; init; }
    public int Qty { get; init; }
    public decimal? Pnl { get; init; }
    public string? SetupName { get; init; }
    public string? HtfTrend { get; init; }
    public string? LtfTrigger { get; init; }
    public bool IsOpen => ExitTime is null;
}
