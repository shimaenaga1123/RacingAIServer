using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Racing.Api.Hubs;
using Racing.Api.Realtime;
using Racing.Api.Services;
using Racing.Application.Auth;
using Racing.Application.Competitions;
using Racing.Application.Connections;
using Racing.Application.Orchestration;
using Racing.Application.Persistence;
using Racing.Domain.Competitions;
using Racing.Infrastructure.Persistence;
using Racing.Infrastructure.Persistence.Repositories;
using Racing.Simulation.Tracks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials();
    });
});

// Database & Persistence configuration
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<RacingDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<RacingDbContext>(options =>
        options.UseInMemoryDatabase("racing_db"));
}

builder.Services.AddScoped<ICompetitionRepository, CompetitionRepository>();
builder.Services.AddScoped<IRaceResultRepository, RaceResultRepository>();
builder.Services.AddSingleton<IReplayRepository, FileReplayRepository>();

// Domain / Simulation / Application services
builder.Services.AddSingleton<ITrackLoader, TrackLoader>();
builder.Services.AddSingleton<IScoringPolicy, StandardScoringPolicy>();
builder.Services.AddSingleton<SessionTokenProvider>();
builder.Services.AddSingleton<CompetitionManager>();
builder.Services.AddSingleton<AgentConnectionRegistry>();
builder.Services.AddSingleton<SpectatorBroadcaster>();
builder.Services.AddSingleton<IRaceOrchestrator, RaceOrchestrator>();
builder.Services.AddOpenApi();

var app = builder.Build();

// 자동 마이그레이션 또는 InMemory DB 초기화
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RacingDbContext>();
    if (db.Database.IsRelational())
    {
        try
        {
            db.Database.Migrate();
        }
        catch
        {
            // DB 컨테이너 미구동 시 개발 편의를 위해 로그 기록 후 계속 진행
        }
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// REST controllers, gRPC, and SignalR endpoints
app.MapControllers();
app.MapGrpcService<RacingGrpcService>();
app.MapHub<RaceHub>("/hubs/race");

app.MapGet("/", () => "Racing AI Server is running. REST: /api/v1/competitions, gRPC: /racing.v1.RacingService, SignalR: /hubs/race");

app.Run();

// For Integration Tests
public partial class Program { }
