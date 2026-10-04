using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Racing.Api.Services;
using Racing.Application.Auth;
using Racing.Application.Competitions;
using Racing.Application.Connections;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddSingleton<SessionTokenProvider>();
builder.Services.AddSingleton<CompetitionManager>();
builder.Services.AddSingleton<AgentConnectionRegistry>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// REST controllers & gRPC endpoint
app.MapControllers();
app.MapGrpcService<RacingGrpcService>();

app.MapGet("/", () => "Racing AI Server is running. REST API at /api/v1/competitions, gRPC at /racing.v1.RacingService");

app.Run();
