namespace AlgoTrading.Domain.Entities;

/// <summary>End-of-session summary + ML metrics (Section 5c iterative loop).</summary>
public class SessionReport
{
    public int Id { get; set; }
    public DateOnly SessionDate { get; set; }
    public int TotalTrades { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int NoTrades { get; set; }
    public decimal GrossPnl { get; set; }
    public decimal MaxDrawdown { get; set; }
    public double WinRate { get; set; }
    /// <summary>Average realized risk:reward across the session.</summary>
    public double AvgRr { get; set; }
    /// <summary>Rolling ML accuracy — must trend UP over sessions.</summary>
    public double RollingAccuracy { get; set; }
    public string? BestSetup { get; set; }
    public string? Notes { get; set; }
}
