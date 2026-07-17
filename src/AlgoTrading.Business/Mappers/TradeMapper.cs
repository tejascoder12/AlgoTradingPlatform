using AlgoTrading.Business.DTOs;
using AlgoTrading.Domain.Entities;

namespace AlgoTrading.Business.Mappers;

/// <summary>Entity &lt;-&gt; DTO mapping for trades and candles.</summary>
public static class TradeMapper
{
    public static TradeDto ToDto(this PaperTrade t) => new()
    {
        Id = t.Id,
        Symbol = t.Symbol,
        Side = t.Side.ToString(),
        EntryTime = t.EntryTime,
        EntryPrice = t.EntryPrice,
        ExitTime = t.ExitTime,
        ExitPrice = t.ExitPrice,
        Qty = t.Qty,
        Pnl = t.Pnl,
        SetupName = t.SetupName,
        HtfTrend = t.HtfTrend,
        LtfTrigger = t.LtfTrigger
    };

    public static OhlcCandleDto ToDto(this OhlcCandle c) => new()
    {
        Time = c.Time,
        Symbol = c.Symbol,
        Interval = c.Interval,
        Open = c.Open,
        High = c.High,
        Low = c.Low,
        Close = c.Close,
        Volume = c.Volume
    };
}
