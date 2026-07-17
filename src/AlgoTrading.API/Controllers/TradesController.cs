using AlgoTrading.Business.DTOs;
using AlgoTrading.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrading.API.Controllers;

/// <summary>Read endpoints for paper trades. Thin — delegates to ITradeService.</summary>
[ApiController]
[Route("api/[controller]")]
public class TradesController : ControllerBase
{
    private readonly ITradeService _tradeService;

    public TradesController(ITradeService tradeService) => _tradeService = tradeService;

    /// <summary>Trades entered on a given date (defaults to today).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TradeDto>>> GetTrades([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var target = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await _tradeService.GetTradesForDateAsync(target, ct));
    }

    /// <summary>A single trade by id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TradeDto>> GetTrade(int id, CancellationToken ct)
    {
        var trade = await _tradeService.GetTradeByIdAsync(id, ct);
        return trade is null ? NotFound() : Ok(trade);
    }
}
