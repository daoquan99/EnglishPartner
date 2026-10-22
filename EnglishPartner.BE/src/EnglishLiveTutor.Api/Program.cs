using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Api.Endpoints;
using EnglishLiveTutor.Api.Hubs;
using EnglishLiveTutor.Api.Realtime;
using EnglishLiveTutor.Api.Services;
using EnglishLiveTutor.Application;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Infrastructure;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 512 * 1024;
    options.EnableDetailedErrors = true;
});

// Configure Authentication & Authorization
builder.Services.AddAuthentication("Bearer")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, EltAuthenticationHandler>("Bearer", null);

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

var app = builder.Build();

// Ensure SQL Server Schema & Seed Starter Data Safely on Startup
try
{
    var dbFactory = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await DatabaseSeeder.EnsureSchemaAndSeedAsync(dbFactory);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Database seeding skipped or deferred until SQL Server is reachable.");
}

app.UseCors("FrontendPolicy");
app.UseMiddleware<JwtAndCsrfMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

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
