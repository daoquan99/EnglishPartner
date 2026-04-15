using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using EnglishPartner.Api.Services;
using EnglishPartner.Api.Endpoints;
using EnglishPartner.Messaging;
using EnglishPartner.Modules.Curriculum.Api;
using EnglishPartner.Modules.Identity.Api;
using EnglishPartner.Modules.Identity.Api.Endpoints;
using EnglishPartner.Modules.Identity.Infrastructure.Persistence;
using EnglishPartner.Modules.Learning.Api;
using EnglishPartner.Modules.Learning.Api.Endpoints;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence;
using EnglishPartner.Modules.AI.Api;
using EnglishPartner.SharedKernel.Interceptors;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Middleware;
using EnglishPartner.SharedKernel.Settings;
using EnglishPartner.SharedKernel.Application.Interceptors;
using EnglishPartner.SharedKernel.Application.BackgroundJobs;
using EnglishPartner.Modules.AI.Infrastructure.Persistence;
using EnglishPartner.Modules.Curriculum.Infrastructure.Persistence;
using EnglishPartner.Modules.Notifications.Api;
using EnglishPartner.Modules.Notifications.Api.Endpoints;
using EnglishPartner.Modules.Notifications.Application.BackgroundJobs;
using EnglishPartner.Modules.Notifications.Application.Settings;
using EnglishPartner.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── MediatR License (Lucky Penny Software) ──
// Set env var BEFORE any AddMediatR calls – MediatR 14.x reads it globally.
var mediatRLicense = builder.Configuration["MediatR:LicenseKey"];
if (!string.IsNullOrWhiteSpace(mediatRLicense))
    Environment.SetEnvironmentVariable("MEDIATR_LICENSE", mediatRLicense);

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

builder.Services.AddAuthorization(options =>
{
    // Role-based: Admin endpoint groups use RequireAuthorization("Admin")
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));

    // Permission-based: individual admin endpoints (only admin JWT has these claims)
    void AddPermission(string code) =>
        options.AddPolicy(code, policy => policy.RequireClaim("permission", code));

    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Curriculum.TopicsWrite);
    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Curriculum.VocabularyWrite);
    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Curriculum.GrammarWrite);
    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Curriculum.ExerciseWrite);
    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Curriculum.ConversationWrite);
    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Curriculum.SentenceWrite);
    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Curriculum.Generate);
    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Learning.AdminView);
    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Learning.AdminWrite);
    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Identity.UsersManage);
    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Identity.RolesManage);
    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Ai.LogsRead);
    AddPermission(EnglishPartner.Modules.Identity.Domain.Roles.Permissions.Notifications.Admin);
});

// ⭐ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// ⭐ Redis Cache ──
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
    var initLogger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        // ── Migrate every module — safe to run multiple times ──
        var identityDb = services.GetRequiredService<EnglishPartner.Modules.Identity.Infrastructure.Persistence.IdentityDbContext>();
        await identityDb.Database.MigrateAsync();

        var aiDb = services.GetRequiredService<EnglishPartner.Modules.AI.Infrastructure.Persistence.AiDbContext>();
        await aiDb.Database.MigrateAsync();

        var curriculumDb = services.GetRequiredService<EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.CurriculumDbContext>();
        await curriculumDb.Database.MigrateAsync();

        var learningDb = services.GetRequiredService<EnglishPartner.Modules.Learning.Infrastructure.Persistence.LearningDbContext>();
        await learningDb.Database.MigrateAsync();

        var notificationDb = services.GetRequiredService<EnglishPartner.Modules.Notifications.Infrastructure.Persistence.NotificationDbContext>();
        await notificationDb.Database.MigrateAsync();

        initLogger.LogInformation("Database migrated successfully.");

        // ⭐ Seed admin user (idempotent — skips if already exists)
        var adminSeeder = services.GetRequiredService<EnglishPartner.Modules.Identity.Infrastructure.Services.AdminUserSeeder>();
        await adminSeeder.SeedAsync(services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());
    }
    catch (Exception ex)
    {
        initLogger.LogError(ex, "An error occurred while migrating the database.");

        // In production, a failed migration should abort startup to avoid running
        // against an inconsistent schema. In development, we allow lenient startup.
        if (!app.Environment.IsDevelopment())
            throw;
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

app.UseCors("AllowFrontend");
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

// ── Admin Endpoints ──
app.MapAdminUserEndpoints();
app.MapAdminLearningEndpoints();
app.MapAdminNotificationEndpoints();


// ── Admin Dashboard ──
app.MapGet("api/v1/admin/dashboard", async (
    EnglishPartner.Modules.Identity.Infrastructure.Persistence.IdentityDbContext identityDb,
    EnglishPartner.Modules.Learning.Infrastructure.Persistence.LearningDbContext  learningDb,
    EnglishPartner.Modules.Notifications.Infrastructure.Persistence.NotificationDbContext notifDb,
    CancellationToken cancellationToken) =>
{
    var now = DateTime.UtcNow;
    var sevenDaysAgo = now.AddDays(-7);

    var totalUsers              = await identityDb.Users.CountAsync(cancellationToken);
    var activeUsersLast7Days    = await identityDb.Users.CountAsync(u => u.UpdatedAt >= sevenDaysAgo, cancellationToken);
    var newRegistrationsLast7   = await identityDb.Users.CountAsync(u => u.CreatedAt >= sevenDaysAgo, cancellationToken);

    var totalSessions           = await learningDb.StudySessions.CountAsync(cancellationToken);
    var completedSessions       = await learningDb.StudySessions
        .CountAsync(s => s.Status == EnglishPartner.Modules.Learning.Domain.StudySessions.SessionStatus.Completed, cancellationToken);
    var avgScore                = await learningDb.StudySessions
        .Where(s => s.Status == EnglishPartner.Modules.Learning.Domain.StudySessions.SessionStatus.Completed)
        .AverageAsync(s => (double?)s.Score, cancellationToken) ?? 0;

    var totalNotifications      = await notifDb.Notifications.CountAsync(cancellationToken);

    return Results.Ok(new
    {
        totalUsers,
        activeUsersLast7Days,
        newRegistrationsLast7Days = newRegistrationsLast7,
        totalSessions,
        totalSessionsCompleted    = completedSessions,
        averageSessionScore       = Math.Round(avgScore, 1),
        totalNotificationsSent    = totalNotifications,
    });
})
.RequireAuthorization("Admin")
.WithTags("Admin — Dashboard");

app.MapGet("/", () => Results.Ok(new
{
    Application = "EnglishPartner API",
    Version = "1.0.0",
    Status = "Running",
    Documentation = "/scalar/v1"
}));

app.Run();
