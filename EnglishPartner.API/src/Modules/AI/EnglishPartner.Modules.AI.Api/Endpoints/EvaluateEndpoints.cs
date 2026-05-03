using EnglishPartner.Modules.AI.Application.Evaluation.Commands.EvaluateAudio;
using EnglishPartner.Modules.AI.Application.Evaluation.Commands.EvaluateInput;
using EnglishPartner.Modules.Curriculum.Contracts.Services;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.AI.Api.Endpoints;

internal static class EvaluateEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/evaluate", async (EvaluateInputRequest request, ISender sender) =>
        {
            var command = new EvaluateInputCommand(
                request.UserInput,
                request.ItemContext,
                request.ItemId,
                request.ItemType);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("EvaluateInput")
        .WithSummary("Evaluate user text input using AI. Returns SM-2 quality score (0-5).")
        .RequireRateLimiting("ai-scoring");

        group.MapPost("/evaluate-audio", async (
            IFormFile audioFile,
            [Microsoft.AspNetCore.Mvc.FromForm] Guid? itemId,
            [Microsoft.AspNetCore.Mvc.FromForm] CurriculumItemType? itemType,
            [Microsoft.AspNetCore.Mvc.FromForm] string? itemContext,
            ISender sender) =>
        {
            if (audioFile is null || audioFile.Length == 0)
                return Results.BadRequest("Audio file is required.");

            const long MaxAudioBytes = 10 * 1024 * 1024; // 10 MB
            if (audioFile.Length > MaxAudioBytes)
                return Results.BadRequest("Audio file is too large. Maximum size is 10 MB.");

            using var stream = audioFile.OpenReadStream();
            var command = new EvaluateAudioCommand(
                stream,
                audioFile.FileName,
                audioFile.ContentType,
                itemContext,
                itemId,
                itemType);

            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("EvaluateAudio")
        .WithSummary("Upload and evaluate an audio file using AI. Returns Transcription and SM-2 quality score (0-5).")
        .DisableAntiforgery()
        .RequireRateLimiting("ai-scoring");
    }
}

public sealed record EvaluateInputRequest(
    string UserInput,
    Guid? ItemId = null,
    CurriculumItemType? ItemType = null,
    string? ItemContext = null);
