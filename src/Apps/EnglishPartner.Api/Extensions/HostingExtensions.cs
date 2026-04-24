using EnglishPartner.Api.Endpoints;
using EnglishPartner.Api.Services;
using EnglishPartner.Messaging;
using EnglishPartner.Modules.AI.Api;
using EnglishPartner.Modules.AI.Infrastructure.Persistence;
using EnglishPartner.Modules.Curriculum.Api;
using EnglishPartner.Modules.Curriculum.Infrastructure.Persistence;
using EnglishPartner.Modules.Identity.Api;
using EnglishPartner.Modules.Identity.Api.Endpoints;
using EnglishPartner.Modules.Identity.Infrastructure.Persistence;
using EnglishPartner.Modules.Learning.Api;
using EnglishPartner.Modules.Learning.Api.Endpoints;
using EnglishPartner.Modules.Learning.Infrastructure.BackgroundJobs;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence;
using EnglishPartner.Modules.Notifications.Api;
using EnglishPartner.Modules.Notifications.Api.Endpoints;
using EnglishPartner.Modules.Notifications.Application.BackgroundJobs;
using EnglishPartner.Modules.Notifications.Application.Settings;
using EnglishPartner.Modules.Notifications.Infrastructure.Persistence;
using EnglishPartner.SharedKernel.Application.Auth;
using EnglishPartner.SharedKernel.Application.BackgroundJobs;
using EnglishPartner.SharedKernel.Application.Interceptors;
using EnglishPartner.SharedKernel.Email;
using EnglishPartner.SharedKernel.Interceptors;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Middleware;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Scalar.AspNetCore;
using StackExchange.Redis;
using System.Text;

namespace EnglishPartner.Api.Extensions;

internal static class HostingExtensions
{
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        // ── MediatR License (Lucky Penny Software) ──
        var mediatRLicense = builder.Configuration["MediatR:LicenseKey"];
        if (!string.IsNullOrWhiteSpace(mediatRLicense))
            Environment.SetEnvironmentVariable("MEDIATR_LICENSE", mediatRLicense);

        // ── Settings (strongly-typed) ──
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
        builder.Services.Configure<GeminiAISettings>(builder.Configuration.GetSection(GeminiAISettings.SectionName));
        builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection(FileStorageSettings.SectionName));
        builder.Services.Configure<RateLimitingSettings>(builder.Configuration.GetSection(RateLimitingSettings.SectionName));
        builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection(CacheSettings.SectionName));
        builder.Services.Configure<OutboxSettings>(builder.Configuration.GetSection(OutboxSettings.SectionName));
        builder.Services.AddEmailSender(builder.Configuration);

        var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
            throw new InvalidOperationException("JWT Secret is missing or too short (< 32 chars). Set 'Jwt:Secret' in appsettings or environment variables.");

        var rateLimitSettings = builder.Configuration.GetSection(RateLimitingSettings.SectionName).Get<RateLimitingSettings>() ?? new RateLimitingSettings();
        var cacheSettings = builder.Configuration.GetSection(CacheSettings.SectionName).Get<CacheSettings>() ?? new CacheSettings();
        CacheSettings.Initialize(cacheSettings); // Initialize the static singleton
        var outboxSettings = builder.Configuration.GetSection(OutboxSettings.SectionName).Get<OutboxSettings>() ?? new OutboxSettings();
        var notificationSettings = builder.Configuration.GetSection(NotificationSettings.SectionName).Get<NotificationSettings>() ?? new NotificationSettings();

        // ── JSON Serialization ──
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        // Đăng ký Provider tự động tạo Policy dựa trên chuỗi Permission (không cần quét Reflection nữa)
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddAuthorization();

        // ⭐ CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
                policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials());
        });

        // Đăng ký Multiplexer để dùng riêng cho việc scan keys
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
        builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString!));

        // ⭐ Redis Cache ──
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
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

            options.AddFixedWindowLimiter("ai-generate", limiter =>
            {
                limiter.PermitLimit = rateLimitSettings.AIGenerate.PermitLimit;
                limiter.Window = TimeSpan.FromSeconds(rateLimitSettings.AIGenerate.WindowSeconds);
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

            if (cleanupInterval > 0)
            {
                config.AddJob<NotificationCleanupJob>(j => j.WithIdentity("NotificationCleanupJob"))
                      .AddTrigger(t => t.ForJob("NotificationCleanupJob")
                          .WithIdentity("NotificationCleanupTrigger")
                          .WithSimpleSchedule(s => s.WithIntervalInHours(cleanupInterval).RepeatForever()));
            }

            config.AddJob<DailyGoalReminderJob>(j => j.WithIdentity("DailyGoalReminderJob"))
                  .AddTrigger(t => t.ForJob("DailyGoalReminderJob")
                      .WithIdentity("DailyGoalReminderTrigger")
                      .WithCronSchedule("0 0 * * * ?"));
        });

        builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        builder.Services.AddSignalR();

        // ── Health Checks ──
        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("Database")!, name: "postgresql")
            .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis");

        // ── Shared Services ──
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(TimeProvider.System);
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

        return builder;
    }

    public static WebApplication ConfigureApplicationPipeline(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("EnglishPartner API");
                options.WithTheme(ScalarTheme.Purple);
                options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });
        }

        app.UseCors("AllowFrontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<EnglishPartner.Modules.Identity.Infrastructure.Middleware.SessionValidationMiddleware>();
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
        app.MapHub<EnglishPartner.Api.Endpoints.Hubs.AiLiveAvatarHub>("/hubs/ai-live-avatar");

        // ── Admin Endpoints ──
        app.MapAdminLearningEndpoints();
        app.MapAdminDashboardEndpoints();

        app.MapGet("/", () => Results.Ok(new
        {
            Application = "EnglishPartner API",
            Version = "1.0.0",
            Status = "Running",
            Documentation = "/scalar/v1"
        }));

        return app;
    }
}
