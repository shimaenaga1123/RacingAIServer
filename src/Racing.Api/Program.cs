using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Racing.Api.Services;
using Racing.Application.Connections;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<AgentConnectionRegistry>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// gRPC endpoint
app.MapGrpcService<RacingGrpcService>();

app.MapGet("/", () => "Racing AI Server is running. Connect via gRPC at /racing.v1.RacingService");

app.Run();
