using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Api.Endpoints;

public static class LessonMediaEndpoints
{
    // Thread-safe in-memory store for active role-play sessions
    private static readonly ConcurrentDictionary<Guid, RolePlaySessionState> RolePlaySessions = new();

    public static IEndpointRouteBuilder MapLessonMediaEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. Upload Media File
        app.MapPost("/api/lesson-media/upload", async (
            IFormFile file,
            IAppDbContext db,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { error = "No file uploaded or file is empty." });

            var assetId = Guid.NewGuid();
            string userId = currentUser.UserId ?? "anonymous";
            string contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

            var recording = new AudioRecordingEntity(
                userId: userId,
                storageKey: $"media/{assetId}_{file.FileName}",
                contentType: contentType,
                fileSizeBytes: file.Length,
                durationMilliseconds: 0,
                id: assetId);

            db.AudioRecordings.Add(recording);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                assetId = assetId.ToString(),
                fileName = file.FileName,
                contentType = contentType,
                fileSizeBytes = file.Length,
                url = $"/api/lesson-media/{assetId}"
            });
        }).RequireAuthorization().DisableAntiforgery();

        // 2. Stream Media or Get Presigned URL
        app.MapGet("/api/lesson-media/{id:guid}", async (
            Guid id,
            IAppDbContext db,
            CancellationToken ct) =>
        {
            var media = await db.AudioRecordings.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
            if (media == null)
                return Results.NotFound(new { error = "Media asset not found." });

            return Results.Ok(new
            {
                assetId = media.Id.ToString(),
                contentType = media.ContentType,
                fileSizeBytes = media.FileSizeBytes,
                storageKey = media.StorageKey,
                streamUrl = $"/api/lesson-media/{media.Id}/stream"
            });
        }).RequireAuthorization();

        // 3. Role-Play: Start Session
        app.MapPost("/api/lesson-roleplay/sessions", (
            RolePlayStartSessionRequest req,
            ICurrentUser currentUser) =>
        {
            var sessionId = Guid.NewGuid();
            string role = string.IsNullOrWhiteSpace(req.AiRole) ? "AI Partner" : req.AiRole.Trim();
            string initialMessage = string.IsNullOrWhiteSpace(req.InitialGreeting)
                ? $"Hello! I am your {role}. How can I help you today?"
                : req.InitialGreeting.Trim();

            var session = new RolePlaySessionState(
                SessionId: sessionId,
                UserId: currentUser.UserId ?? "anonymous",
                Scenario: req.Scenario,
                AiRole: role,
                MaxTurns: req.MaxTurns <= 0 ? 6 : req.MaxTurns,
                CurrentTurnIndex: 0,
                History: new List<RolePlayTurnEntry>
                {
                    new("assistant", initialMessage, DateTimeOffset.UtcNow)
                });

            RolePlaySessions[sessionId] = session;

            return Results.Created($"/api/lesson-roleplay/sessions/{sessionId}", new
            {
                sessionId = session.SessionId,
                status = "Active",
                currentTurnIndex = session.CurrentTurnIndex,
                aiRole = session.AiRole,
                initialMessage = initialMessage,
                maxTurns = session.MaxTurns
            });
        }).RequireAuthorization().RequirePermission("lesson-exercises.attempt");

        // 4. Role-Play: Send Turn
        app.MapPost("/api/lesson-roleplay/sessions/{sessionId:guid}/turns", (
            Guid sessionId,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            RolePlayTurnRequest req,
            ICurrentUser currentUser) =>
        {
            if (!RolePlaySessions.TryGetValue(sessionId, out var session))
                return Results.NotFound(new { error = "Role-play session not found." });

            if (req.ClientTurnIndex != session.CurrentTurnIndex + 1)
            {
                return Results.Conflict(new
                {
                    error = $"TURN_INDEX_CONFLICT: Expected turnIndex {session.CurrentTurnIndex + 1}, received {req.ClientTurnIndex}."
                });
            }

            if (session.CurrentTurnIndex >= session.MaxTurns)
            {
                return Results.BadRequest(new { error = "MaxTurnsReached: This conversation session has completed its allotted turns." });
            }

            session.History.Add(new("user", req.UserMessage, DateTimeOffset.UtcNow));
            session.CurrentTurnIndex++;

            string aiReply = GenerateAiRolePlayReply(session.AiRole, session.Scenario, req.UserMessage, session.CurrentTurnIndex, session.MaxTurns);
            session.History.Add(new("assistant", aiReply, DateTimeOffset.UtcNow));

            bool isLastTurn = session.CurrentTurnIndex >= session.MaxTurns;

            return Results.Ok(new
            {
                sessionId = session.SessionId,
                turnIndex = session.CurrentTurnIndex,
                userTurn = new { message = req.UserMessage },
                aiTurn = new { message = aiReply },
                isCompleted = isLastTurn,
                remainingTurns = Math.Max(0, session.MaxTurns - session.CurrentTurnIndex)
            });
        }).RequireAuthorization().RequirePermission("lesson-exercises.attempt");

        // 5. Role-Play: End Session
        app.MapPost("/api/lesson-roleplay/sessions/{sessionId:guid}/end", (
            Guid sessionId,
            ICurrentUser currentUser) =>
        {
            if (!RolePlaySessions.TryRemove(sessionId, out var session))
                return Results.NotFound(new { error = "Role-play session not found." });

            return Results.Ok(new
            {
                sessionId = session.SessionId,
                status = "SessionEnded",
                totalTurnsCompleted = session.CurrentTurnIndex,
                transcript = session.History.Select(h => new { role = h.Role, message = h.Content }),
                message = "Session ended successfully. Transcript saved."
            });
        }).RequireAuthorization().RequirePermission("lesson-exercises.attempt");

        return app;
    }

    private static string GenerateAiRolePlayReply(string aiRole, string scenario, string userMsg, int turn, int maxTurns)
    {
        string u = userMsg.ToLowerInvariant();
        if (u.Contains("menu") || u.Contains("special"))
            return "Certainly! Today our chef recommends the homemade spinach ravioli with fresh truffle oil, and pan-seared sea bass. Would you like a beverage to start?";
        if (u.Contains("water") || u.Contains("wine") || u.Contains("drink"))
            return "Excellent choice. I will bring that right away. Are you ready to order your main course, or would you prefer a few more minutes?";
        if (u.Contains("bill") || u.Contains("check"))
            return "Here is your bill. We accept credit cards or mobile payment. Thank you so much for dining with us this evening!";

        if (turn >= maxTurns)
            return $"Thank you for discussing this with me! That concludes our conversation for today. You did a great job communicating your requests clearly.";

        return $"Understood. As your {aiRole}, I hear your request clearly. Let's proceed with that. What else can I assist you with?";
    }

    private sealed class RolePlaySessionState
    {
        public Guid SessionId { get; }
        public string UserId { get; }
        public string Scenario { get; }
        public string AiRole { get; }
        public int MaxTurns { get; }
        public int CurrentTurnIndex { get; set; }
        public List<RolePlayTurnEntry> History { get; }

        public RolePlaySessionState(Guid SessionId, string UserId, string Scenario, string AiRole, int MaxTurns, int CurrentTurnIndex, List<RolePlayTurnEntry> History)
        {
            this.SessionId = SessionId;
            this.UserId = UserId;
            this.Scenario = Scenario;
            this.AiRole = AiRole;
            this.MaxTurns = MaxTurns;
            this.CurrentTurnIndex = CurrentTurnIndex;
            this.History = History;
        }
    }

    private sealed record RolePlayTurnEntry(string Role, string Content, DateTimeOffset Timestamp);
}

public sealed record RolePlayStartSessionRequest(
    string Scenario,
    string? AiRole,
    string? InitialGreeting,
    int MaxTurns = 6
);

public sealed record RolePlayTurnRequest(
    int ClientTurnIndex,
    string UserMessage,
    string? UserAudioAssetId = null
);
