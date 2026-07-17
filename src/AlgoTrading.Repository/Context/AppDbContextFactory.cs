using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlgoTrading.Repository.Context;

/// <summary>
/// Design-time DbContext factory so `dotnet ef` can create/apply migrations
/// WITHOUT building the API startup project (handy when the API is running and
/// its DLLs are locked). Used only by EF tooling, never at runtime.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Real local creds come from the ALGO_DB_CONNECTION env var (or
        // appsettings.Development.json at runtime). Fallback = docker-compose default.
        var conn = Environment.GetEnvironmentVariable("ALGO_DB_CONNECTION")
                   ?? "Host=localhost;Port=5432;Database=algotrading;Username=algo;Password=algo_dev_pwd";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(conn, b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name))
            .Options;
        return new AppDbContext(options);
    }
}
