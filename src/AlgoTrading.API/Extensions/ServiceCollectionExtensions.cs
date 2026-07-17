using AlgoTrading.Business.Interfaces;
using AlgoTrading.Business.Services;
using AlgoTrading.Repository.Cache;
using AlgoTrading.Repository.Context;
using AlgoTrading.Repository.Interfaces;
using AlgoTrading.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace AlgoTrading.API.Extensions;

/// <summary>Central DI registration — keeps Program.cs clean (CLAUDE.md §4).</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers EF Core (Postgres/TimescaleDB) + Redis connections (code-first).</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var postgres = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(postgres))
            throw new InvalidOperationException(
                "Missing connection string 'ConnectionStrings:DefaultConnection' in configuration.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(postgres, npgsql =>
                // Migrations live in the Repository project, not the API startup project.
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

        // Single shared Redis multiplexer.
        var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConn));

        return services;
    }

    /// <summary>
    /// Applies pending EF migrations on startup (code-first convenience).
    /// Logs and continues if the DB is unreachable so the API still boots
    /// (e.g. when the Docker DB stack is not running yet).
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Could not apply migrations at startup (is the TimescaleDB stack up?). Continuing.");
        }
    }

    /// <summary>Registers repositories (data-access layer).</summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<IMarketDataRepository, MarketDataRepository>();
        services.AddScoped<IRedisCacheService, RedisCacheService>();
        return services;
    }

    /// <summary>Registers business services.</summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<ITradeService, TradeService>();
        services.AddScoped<IMarketDataService, MarketDataService>();
        return services;
    }
}
