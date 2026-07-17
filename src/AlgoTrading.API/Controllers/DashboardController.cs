using AlgoTrading.Business.DTOs;
using AlgoTrading.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrading.API.Controllers;

/// <summary>Dashboard summary endpoints (P&amp;L + engine status).</summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ITradeService _tradeService;
    private readonly IMarketDataService _marketDataService;

    public DashboardController(ITradeService tradeService, IMarketDataService marketDataService)
    {
        _tradeService = tradeService;
        _marketDataService = marketDataService;
    }

    /// <summary>Aggregated P&amp;L summary for the dashboard header (Round 1 vertical slice).</summary>
    [HttpGet("pnl-summary")]
    public async Task<ActionResult<PnlSummaryDto>> GetPnlSummary(CancellationToken ct)
        => Ok(await _tradeService.GetPnlSummaryAsync(ct));

    /// <summary>Today's (IST) session summary — P&amp;L, win rate, best/worst trade.</summary>
    [HttpGet("today-summary")]
    public async Task<ActionResult<TodaySummaryDto>> GetTodaySummary(CancellationToken ct)
        => Ok(await _tradeService.GetTodaySummaryAsync(ct));

    /// <summary>Live engine LIVE/OFFLINE status from the Redis heartbeat.</summary>
    [HttpGet("engine-status")]
    public async Task<ActionResult<EngineStatusDto>> GetEngineStatus(CancellationToken ct)
        => Ok(await _marketDataService.GetEngineStatusAsync(ct));
}
