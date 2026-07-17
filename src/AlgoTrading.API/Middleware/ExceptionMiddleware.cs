using System.Net;
using System.Text.Json;

namespace AlgoTrading.API.Middleware;

/// <summary>Converts unhandled exceptions into a consistent JSON error response.</summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var payload = JsonSerializer.Serialize(new
            {
                error = "An unexpected error occurred.",
                traceId = context.TraceIdentifier
            });
            await context.Response.WriteAsync(payload);
        }
    }
}
