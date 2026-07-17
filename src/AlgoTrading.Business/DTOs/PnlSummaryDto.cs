namespace AlgoTrading.Business.DTOs;

/// <summary>Aggregated P&amp;L summary shown on the dashboard.</summary>
public record PnlSummaryDto
{
    public int TotalTrades { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int OpenPositions { get; init; }
    public decimal GrossPnl { get; init; }
    public decimal RealizedPnl { get; init; }
    /// <summary>Win rate as a fraction 0..1.</summary>
    public double WinRate { get; init; }
    public decimal BestTradePnl { get; init; }
    public decimal WorstTradePnl { get; init; }
}
