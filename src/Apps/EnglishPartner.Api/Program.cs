using EnglishPartner.Api.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog (reads full config from appsettings.json "Serilog" section) ──
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

builder.AddApplicationServices();

var app = builder.Build();

app.ConfigureApplicationPipeline();
await app.InitializeDatabaseAsync();

app.Run();
