namespace AlgoTrading.Domain.Entities;

/// <summary>WHY a trade lost — the ML goldmine (Section 5c).</summary>
public class LossAttribution
{
    public int Id { get; set; }

    /// <summary>FK to the losing paper trade.</summary>
    public int TradeId { get; set; }
    public PaperTrade? Trade { get; set; }

    /// <summary>Cause: wrong_direction / chop / theta / stop_hit / structure_break.</summary>
    public string Cause { get; set; } = string.Empty;
    public decimal RealizedLoss { get; set; }
    public string? ContextJson { get; set; }
}
