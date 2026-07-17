namespace AlgoTrading.Domain.Entities;

/// <summary>A logged decision to SIT OUT. Avoiding a loss counts as edge (Section 5c).</summary>
public class NoTrade
{
    public int Id { get; set; }
    public DateTimeOffset Time { get; set; }
    /// <summary>Why we sat out: chop / low_volume / outside_window / dte_risk.</summary>
    public string Reason { get; set; } = string.Empty;
    public string? ContextJson { get; set; }
}
