using AlgoTrading.Business.Services;
using AlgoTrading.Domain.Entities;
using AlgoTrading.Domain.Enums;
using AlgoTrading.Repository.Interfaces;
using Moq;

namespace AlgoTrading.Tests;

public class TradeServiceTests
{
    [Fact]
    public async Task GetPnlSummary_ComputesWinsLossesAndRates()
    {
        // Arrange: 2 wins, 1 loss, 1 still open.
        var trades = new List<PaperTrade>
        {
            Closed(id: 1, pnl: 500m),
            Closed(id: 2, pnl: 300m),
            Closed(id: 3, pnl: -200m),
            Open(id: 4)
        };

        var repo = new Mock<ITradeRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trades);

        var service = new TradeService(repo.Object);

        // Act
        var summary = await service.GetPnlSummaryAsync();

        // Assert
        Assert.Equal(4, summary.TotalTrades);
        Assert.Equal(2, summary.Wins);
        Assert.Equal(1, summary.Losses);
        Assert.Equal(1, summary.OpenPositions);
        Assert.Equal(600m, summary.GrossPnl);        // 500+300-200
        Assert.Equal(600m, summary.RealizedPnl);
        Assert.Equal(2d / 3d, summary.WinRate, 3);   // 2 wins of 3 closed
        Assert.Equal(500m, summary.BestTradePnl);
        Assert.Equal(-200m, summary.WorstTradePnl);
    }

    private static PaperTrade Closed(int id, decimal pnl) => new()
    {
        Id = id,
        Symbol = "NIFTY",
        Side = TradeSide.Buy,
        EntryTime = DateTimeOffset.UtcNow.AddHours(-1),
        EntryPrice = 100m,
        ExitTime = DateTimeOffset.UtcNow,
        ExitPrice = 100m + pnl,
        Qty = 1,
        Pnl = pnl
    };

    private static PaperTrade Open(int id) => new()
    {
        Id = id,
        Symbol = "NIFTY",
        Side = TradeSide.Buy,
        EntryTime = DateTimeOffset.UtcNow,
        EntryPrice = 100m,
        Qty = 1
    };
}
