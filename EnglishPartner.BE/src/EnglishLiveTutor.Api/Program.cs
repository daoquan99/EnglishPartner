using System.Diagnostics;
using System.Net;
using EnglishLiveTutor.Api.Common;
using EnglishLiveTutor.Api.Exceptions;
using System.Security.Claims;
using System.Threading.RateLimiting;
using EnglishLiveTutor.Api.Configuration;
using EnglishLiveTutor.Api.Endpoints;
using EnglishLiveTutor.Api.Hubs;
using EnglishLiveTutor.Api.Realtime;
using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Api.Services;
using EnglishLiveTutor.Application;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Identity.Commands.RefreshToken;
using EnglishLiveTutor.Infrastructure;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Storage;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Current User & HttpContext Accessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<IPermissionChecker, ClaimsPermissionChecker>();

// Register Clean Architecture Core Layers
builder.Services.AddApplicationServices(builder.Configuration["MediatR:LicenseKey"]);

// Register Realtime Notifier Adapter for SignalR (Singleton to match IHubContext and GeminiLiveConnectionManager)
builder.Services.AddSingleton<ILiveSessionRealtimeNotifier, SignalRLiveSessionRealtimeNotifier>();

builder.Services.AddInfrastructureServices(builder.Configuration);

int refreshGraceSeconds = builder.Configuration.GetValue<int?>("Auth:RefreshReuseGraceSeconds") ?? 10;
builder.Services.AddSingleton(new RefreshTokenOptions(refreshGraceSeconds));

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://127.0.0.1:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("X-Total-Count", "X-Page", "X-Page-Size", "X-Total-Pages");
    });
});

// Configure SignalR
builder.Services.AddAppSignalR(builder.Configuration, builder.Environment.IsDevelopment());

// Configure Authentication & Authorization
builder.Services.AddAuthentication("Bearer")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, EltAuthenticationHandler>("Bearer", null);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
        string traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["traceId"] = traceId;
        if (!string.IsNullOrWhiteSpace(context.ProblemDetails.Detail))
        {
            context.ProblemDetails.Extensions["error"] = context.ProblemDetails.Detail;
        }
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        await ApiProblem.WriteResponseAsync(
            context.HttpContext.Response,
            StatusCodes.Status429TooManyRequests,
            "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau giây lát.",
            cancellationToken: token);
    };

    options.AddConfiguredRateLimiters(builder.Configuration);

    int aiTokenLimit = builder.Configuration.GetValue<int?>("RateLimiting:Ai:TokenLimit") ?? 20;
    int aiTokensPerPeriod = builder.Configuration.GetValue<int?>("RateLimiting:Ai:TokensPerPeriod") ?? 20;
    int aiPeriodSeconds = builder.Configuration.GetValue<int?>("RateLimiting:Ai:ReplenishmentPeriodSeconds") ?? 60;

    options.AddPolicy("ai", httpContext =>
    {
        string userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub")
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: userId,
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = aiTokenLimit,
                TokensPerPeriod = aiTokensPerPeriod,
                ReplenishmentPeriod = TimeSpan.FromSeconds(aiPeriodSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

// Eagerly validate all required configuration options and token service before database operations
_ = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
_ = app.Services.GetRequiredService<IOptions<LessonGenerationOptions>>().Value;
_ = app.Services.GetRequiredService<IOptions<LessonAssessmentOptions>>().Value;
_ = app.Services.GetRequiredService<IOptions<LessonRolePlayOptions>>().Value;
_ = app.Services.GetRequiredService<IOptions<LessonMediaStorageOptions>>().Value;
_ = app.Services.GetRequiredService<IOptions<AntigravityCliOptions>>().Value;
_ = app.Services.GetRequiredService<IJwtTokenService>();

// Ensure SQL Server Schema & Seed Starter Data Safely on Startup
bool migrateOnStartup = app.Configuration.GetValue<bool>("Database:MigrateOnStartup");
bool seedOnStartup = app.Configuration.GetValue<bool>("Database:SeedOnStartup");

if (migrateOnStartup || seedOnStartup)
{
    try
    {
        var dbFactory = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var adminOptions = app.Services.GetService<IOptions<AdminUserSeedOptions>>()?.Value;
        var demoOptions = app.Services.GetService<IOptions<DemoLearnerSeedOptions>>()?.Value;
        await DatabaseSeeder.EnsureSchemaAndSeedAsync(
            dbFactory,
            adminOptions: adminOptions,
            demoLearnerOptions: demoOptions,
            environment: app.Environment,
            logger: app.Logger,
            migrate: migrateOnStartup,
            seed: seedOnStartup);
    }
    catch (Exception ex)
    {
        if (app.Environment.IsDevelopment())
        {
            app.Logger.LogWarning(ex, "Database migration or seeding skipped or deferred until SQL Server is reachable.");
        }
        else
        {
            app.Logger.LogError(ex, "Database migration or seeding failed in non-Development environment.");
            throw;
        }
    }
}

// Forwarded Headers (X-Forwarded-For, X-Forwarded-Proto) before authentication
app.UseConfiguredForwardedHeaders(app.Configuration, app.Logger);

app.UseExceptionHandler();

app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("FrontendPolicy");
app.UseMiddleware<CsrfHeaderMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Map Minimal API Endpoints (CQRS Handlers)
app.MapHealthEndpoints();
app.MapIdentityEndpoints();
app.MapPromptEndpoints();
app.MapVoiceEndpoints();
app.MapTopicEndpoints();
app.MapConversationEndpoints();
app.MapVocabularyEndpoints();
app.MapDailyLearningEndpoints();
app.MapPronunciationEndpoints();
app.MapAiModelEndpoints();
app.MapLessonEndpoints();
app.MapLessonAttemptEndpoints();
app.MapLessonRubricEndpoints();
app.MapLessonMediaEndpoints();
app.MapVocabularyGamesEndpoints();

// Map Realtime SignalR Hubs
app.MapHub<TutorConversationHub>("/hubs/tutor").RequireAuthorization();
app.MapHub<TranscribeLiveHub>("/hubs/transcribe").RequireAuthorization();
app.MapHub<TranslateLiveHub>("/hubs/translate").RequireAuthorization();

app.Run();

public partial class Program { }

