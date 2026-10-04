using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Racing.Api.Hubs;
using Racing.Api.Realtime;
using Racing.Api.Services;
using Racing.Application.Auth;
using Racing.Application.Competitions;
using Racing.Application.Connections;

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

builder.Services.AddSingleton<SessionTokenProvider>();
builder.Services.AddSingleton<CompetitionManager>();
builder.Services.AddSingleton<AgentConnectionRegistry>();
builder.Services.AddSingleton<SpectatorBroadcaster>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

// REST controllers, gRPC, and SignalR endpoints
app.MapControllers();
app.MapGrpcService<RacingGrpcService>();
app.MapHub<RaceHub>("/hubs/race");

app.MapGet("/", () => "Racing AI Server is running. REST: /api/v1/competitions, gRPC: /racing.v1.RacingService, SignalR: /hubs/race");

app.Run();
