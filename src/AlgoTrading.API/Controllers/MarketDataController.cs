using AlgoTrading.Business.DTOs;
using AlgoTrading.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrading.API.Controllers;

/// <summary>Read endpoints for time-series market data (charts).</summary>
[ApiController]
[Route("api/[controller]")]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;

    public MarketDataController(IMarketDataService marketDataService)
        => _marketDataService = marketDataService;

    /// <summary>Recent OHLC candles for a symbol/interval, chronological order.</summary>
    [HttpGet("candles")]
    public async Task<ActionResult<IReadOnlyList<OhlcCandleDto>>> GetCandles(
        [FromQuery] string symbol,
        [FromQuery] string interval = "1m",
        [FromQuery] int limit = 200,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("symbol is required.");
        limit = Math.Clamp(limit, 1, 1000);
        return Ok(await _marketDataService.GetRecentCandlesAsync(symbol, interval, limit, ct));
    }
}
