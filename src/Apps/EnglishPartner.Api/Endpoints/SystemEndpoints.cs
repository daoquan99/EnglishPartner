using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.SharedKernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics;


namespace EnglishPartner.Api.Endpoints;

internal static class SystemEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/system")
            .WithTags("System");
            // No global auth required here, we check it inside optionally, or we can enforce it.
            // Let's require authorization to upload files.

        group.MapPost("upload", async (
            [FromForm] IFormFile file,
            [FromForm] string folder,
            IFileStorageService fileStorage,
            CancellationToken cancellationToken) =>
        {
            if (file == null || file.Length == 0) return Results.BadRequest("File is empty.");
            if (string.IsNullOrWhiteSpace(folder)) folder = "general";

            // Enforce basic security
            if (folder.Contains("..") || folder.Contains("/")) return Results.BadRequest("Invalid folder name.");

            await using var stream = file.OpenReadStream();
            var url = await fileStorage.UploadAsync(stream, file.FileName, folder, cancellationToken);
            
            return Results.Ok(new { Url = url });
        })
        .DisableAntiforgery()
        .RequireAuthorization()
        .WithSummary("Upload a file (Admin/User). Valid folders: avatars, topics, audio, general.");

        // ── AI Health Check ──
        group.MapGet("/ai-ping", async (IGeminiService gemini, CancellationToken ct) =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                // Minimal prompt — just checks connectivity + API key validity
                var response = await gemini.ChatAsync(
                    systemPrompt: "You are a ping checker. Reply with exactly: PONG",
                    history: [new EnglishPartner.Modules.AI.Application.Abstractions.ChatTurn("user", "ping")],
                    userMessage: "ping",
                    cancellationToken: ct);

                sw.Stop();
                return Results.Ok(new
                {
                    status    = "ok",
                    response  = response.Trim(),
                    latencyMs = sw.ElapsedMilliseconds,
                    timestamp = DateTime.UtcNow,
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Results.Ok(new
                {
                    status    = "error",
                    error     = ex.Message,
                    latencyMs = sw.ElapsedMilliseconds,
                    timestamp = DateTime.UtcNow,
                });
            }
        })
        // .RequireAuthorization()  // must be logged in — prevent public abuse
        .WithName("AiPing")
        .WithSummary("Test AI connectivity. Sends a minimal prompt to Gemini and returns PONG + latency.");
    }
}
