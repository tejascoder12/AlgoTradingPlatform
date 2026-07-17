using AlgoTrading.API.Extensions;
using AlgoTrading.API.Hubs;
using AlgoTrading.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// --- Services (DI wired via ServiceCollectionExtensions, CLAUDE.md §4) ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddBusinessServices();

// Angular dev server CORS.
const string CorsPolicy = "AngularDev";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

// Code-first: apply any pending migrations on startup (safe if DB is down).
await app.ApplyMigrationsAsync();

// --- HTTP pipeline ---
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthorization();

app.MapControllers();
app.MapHub<LiveFeedHub>("/hubs/live-feed");

app.Run();
