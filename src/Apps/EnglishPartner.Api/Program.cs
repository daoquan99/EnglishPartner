using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using EnglishPartner.Api.Services;
using EnglishPartner.Api.Endpoints;
using EnglishPartner.Messaging;
using EnglishPartner.Modules.Curriculum.Api;
using EnglishPartner.Modules.Identity.Api;
using EnglishPartner.Modules.Learning.Api;
using EnglishPartner.Modules.AI.Api;
using EnglishPartner.SharedKernel.Interceptors;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Middleware;
using EnglishPartner.SharedKernel.Settings;
using EnglishPartner.SharedKernel.Application.Interceptors;
using EnglishPartner.SharedKernel.Application.BackgroundJobs;
using EnglishPartner.Modules.AI.Infrastructure.Persistence;
using EnglishPartner.Modules.Curriculum.Infrastructure.Persistence;
using EnglishPartner.Modules.Identity.Infrastructure.Persistence;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence;
using EnglishPartner.Modules.Notifications.Api;
using EnglishPartner.Modules.Notifications.Api.Endpoints;
using EnglishPartner.Modules.Notifications.Application.BackgroundJobs;
using EnglishPartner.Modules.Notifications.Application.Settings;
using EnglishPartner.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog (reads full config from appsettings.json "Serilog" section) ──
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// ── Settings (strongly-typed) ──
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<GeminiAISettings>(builder.Configuration.GetSection(GeminiAISettings.SectionName));
builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection(FileStorageSettings.SectionName));
builder.Services.Configure<RateLimitingSettings>(builder.Configuration.GetSection(RateLimitingSettings.SectionName));
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection(CacheSettings.SectionName));
builder.Services.Configure<OutboxSettings>(builder.Configuration.GetSection(OutboxSettings.SectionName));

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? new JwtSettings();

if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
    throw new InvalidOperationException(
        "JWT Secret is missing or too short (< 32 chars). Set 'Jwt:Secret' in appsettings or environment variables.");

var rateLimitSettings = builder.Configuration
    .GetSection(RateLimitingSettings.SectionName)
    .Get<RateLimitingSettings>() ?? new RateLimitingSettings();

var cacheSettings = builder.Configuration
    .GetSection(CacheSettings.SectionName)
    .Get<CacheSettings>() ?? new CacheSettings();

var outboxSettings = builder.Configuration
    .GetSection(OutboxSettings.SectionName)
    .Get<OutboxSettings>() ?? new OutboxSettings();

var notificationSettings = builder.Configuration
    .GetSection(NotificationSettings.SectionName)
    .Get<NotificationSettings>() ?? new NotificationSettings();

// ── OpenAPI ──
builder.Services.AddOpenApi();

// ── Authentication (JWT) ──
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Redis Cache ──
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = cacheSettings.InstanceName;
});

// ── Rate Limiting ──
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("ai-chat", limiter =>
    {
        limiter.PermitLimit = rateLimitSettings.AIChat.PermitLimit;
        limiter.Window = TimeSpan.FromSeconds(rateLimitSettings.AIChat.WindowSeconds);
    });

    options.AddFixedWindowLimiter("ai-scoring", limiter =>
    {
        limiter.PermitLimit = rateLimitSettings.AIScoring.PermitLimit;
        limiter.Window = TimeSpan.FromSeconds(rateLimitSettings.AIScoring.WindowSeconds);
    });
});

// ── Quartz Background Jobs ──
builder.Services.AddQuartz(config =>
{
    var pollInterval = outboxSettings.PollingIntervalSeconds;
    var cleanupInterval = notificationSettings.CleanupIntervalHours;

    void AddOutboxJob<TJob>(string name) where TJob : IJob
    {
        config.AddJob<TJob>(j => j.WithIdentity(name))
              .AddTrigger(t => t.ForJob(name)
                  .WithIdentity($"{name}Trigger")
                  .WithSimpleSchedule(s => s.WithIntervalInSeconds(pollInterval).RepeatForever()));
    }

    AddOutboxJob<ProcessOutboxMessagesJob<AiDbContext>>("ProcessOutboxJobAI");
    AddOutboxJob<ProcessOutboxMessagesJob<CurriculumDbContext>>("ProcessOutboxJobCurriculum");
    AddOutboxJob<ProcessOutboxMessagesJob<IdentityDbContext>>("ProcessOutboxJobIdentity");
    AddOutboxJob<ProcessOutboxMessagesJob<LearningDbContext>>("ProcessOutboxJobLearning");
    AddOutboxJob<ProcessOutboxMessagesJob<NotificationDbContext>>("ProcessOutboxJobNotifications");

    // Notification cleanup — interval driven by appsettings "Notifications:CleanupIntervalHours"
    if (cleanupInterval > 0)
    {
        config.AddJob<NotificationCleanupJob>(j => j.WithIdentity("NotificationCleanupJob"))
              .AddTrigger(t => t.ForJob("NotificationCleanupJob")
                  .WithIdentity("NotificationCleanupTrigger")
                  .WithSimpleSchedule(s => s.WithIntervalInHours(cleanupInterval).RepeatForever()));
    }
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

// ── SignalR ──
builder.Services.AddSignalR();

// ── Health Checks ──
builder.Services
    .AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!, name: "postgresql")
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis");

// ── Shared Services ──
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<AuditableEntityInterceptor>();
builder.Services.AddScoped<ConvertDomainEventsToOutboxMessagesInterceptor>();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// ── API Versioning ──
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// ── Module Registration ──
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddAiModule(builder.Configuration);
builder.Services.AddCurriculumModule(builder.Configuration);
builder.Services.AddLearningModule(builder.Configuration);
builder.Services.AddNotificationModule(builder.Configuration);

var app = builder.Build();

// ── Auto-Initialize Database (Development ONLY) ──
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        // Creates the database and all schemas/tables if they do not exist.
        // Bypasses the need for manual EF Migrations during initial development.
        var identityDb = services.GetRequiredService<EnglishPartner.Modules.Identity.Infrastructure.Persistence.IdentityDbContext>();
        await identityDb.Database.EnsureCreatedAsync();

        var aiDb = services.GetRequiredService<EnglishPartner.Modules.AI.Infrastructure.Persistence.AiDbContext>();
        await aiDb.Database.EnsureCreatedAsync();

        var curriculumDb = services.GetRequiredService<EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.CurriculumDbContext>();
        await curriculumDb.Database.EnsureCreatedAsync();

        var learningDb = services.GetRequiredService<EnglishPartner.Modules.Learning.Infrastructure.Persistence.LearningDbContext>();
        await learningDb.Database.EnsureCreatedAsync();

        var notificationDb = services.GetRequiredService<EnglishPartner.Modules.Notifications.Infrastructure.Persistence.NotificationDbContext>();
        await notificationDb.Database.EnsureCreatedAsync();
        
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database initialized and missing schemas created.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// ── Middleware Pipeline ──
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("EnglishPartner API");
        options.WithTheme(ScalarTheme.BluePlanet);
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        // Scalar will automatically detect the Bearer scheme from the OpenAPI doc 
        // and show the Authorization input box.
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseStaticFiles();

app.MapHealthChecks("/health");

// ── Module Endpoints ──
app.UseIdentityModule();
app.MapAiEndpoints();
app.UseCurriculumModule();
app.UseLearningModule();
app.MapNotificationEndpoints();
SystemEndpoints.Map(app);

app.MapGet("/", () => Results.Ok(new
{
    Application = "EnglishPartner API",
    Version = "1.0.0",
    Status = "Running",
    Documentation = "/scalar/v1"
}));

app.Run();
