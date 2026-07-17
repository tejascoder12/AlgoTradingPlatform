namespace AlgoTrading.Business.DTOs;

/// <summary>Today's (IST trading date) session summary — shown alongside the all-time P&amp;L.</summary>
public record TodaySummaryDto
{
    /// <summary>The IST trading date these numbers cover.</summary>
    public DateOnly SessionDate { get; init; }
    public int TotalTrades { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int OpenPositions { get; init; }
    public decimal GrossPnl { get; init; }
    public decimal RealizedPnl { get; init; }
    /// <summary>Win rate as a fraction 0..1 over today's closed trades.</summary>
    public double WinRate { get; init; }
    public decimal BestTradePnl { get; init; }
    public decimal WorstTradePnl { get; init; }
    /// <summary>Symbol of today's best trade, null when nothing has closed yet.</summary>
    public string? BestTradeSymbol { get; init; }
    /// <summary>Symbol of today's worst trade, null when nothing has closed yet.</summary>
    public string? WorstTradeSymbol { get; init; }
    /// <summary>False when no trade has been entered today — lets the UI say so explicitly.</summary>
    public bool HasTraded { get; init; }
}
