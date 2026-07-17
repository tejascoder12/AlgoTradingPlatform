using Microsoft.AspNetCore.SignalR;

namespace AlgoTrading.API.Hubs;

/// <summary>
/// SignalR hub that pushes live P&amp;L / spot / signals to the dashboard.
/// A background broadcaster (added later) reads Redis and calls Clients.All.
/// The hub itself never calls into the trading engine (CLAUDE.md §2).
/// </summary>
public class LiveFeedHub : Hub
{
    /// <summary>Well-known client method names the frontend subscribes to.</summary>
    public const string PnlUpdate = "PnlUpdate";
    public const string EngineStatus = "EngineStatus";
    public const string SignalUpdate = "SignalUpdate";
}
