using EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveConversation;
using EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveExampleSentences;
using EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveExercise;
using EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveGrammar;
using EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveVocabulary;
using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Curriculum.Api.Endpoints;

/// <summary>
/// Admin-only endpoints for AI-driven curriculum content generation.
/// All endpoints save results immediately (IsAiGenerated=true) for admin review afterward.
/// Supported AI exercise types: MultipleChoice, FillInTheBlank, TrueFalse, Translation,
///   SentenceCompletion, WordForm, VerbTense, Paraphrasing, GapFilling,
///   ReorderingWords, DialogueCompletion, ShortAnswerQuestions.
/// </summary>
internal static class CurriculumGenerateEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("api/v1/admin/curriculum/generate")
            .WithTags("Curriculum AI Generate")
            .RequireAuthorization("Admin");

        // ── Vocabulary ─────────────────────────────────────────────────────

        admin.MapPost("/vocabulary", async (GenerateVocabularyRequest request, ISender sender) =>
        {
            var command = new GenerateAndSaveVocabularyCommand(
                request.WordHint,
                request.Count,
                request.Level,
                request.TopicId,
                request.AutoGenerateExamples,
                request.ExampleCount,
                request.ExampleTargetLanguageCodes);

            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GenerateAndSaveVocabulary")
        .WithSummary("AI: Generate N vocabulary entries with optional auto-generated example sentences, then save. Returns list.")
        .RequireAuthorization("curriculum.generate");

        // ── Grammar ────────────────────────────────────────────────────────

        admin.MapPost("/grammar", async (GenerateGrammarRequest request, ISender sender) =>
        {
            var command = new GenerateAndSaveGrammarCommand(
                request.Title,
                request.Level,
                request.TopicId,
                request.ExampleCount,
                request.TargetLanguageCodes,
                request.Count);

            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GenerateAndSaveGrammar")
        .WithSummary("AI: Generate N grammar rules with examples translated into TargetLanguageCodes, then save. Returns list.")
        .RequireAuthorization("curriculum.generate");

        // ── Conversation ───────────────────────────────────────────────────

        admin.MapPost("/conversation", async (GenerateConversationRequest request, ISender sender) =>
        {
            var command = new GenerateAndSaveConversationCommand(
                request.TopicName,
                request.Level,
                request.Scenario,
                request.LineCount,
                request.TargetLanguageCodes,
                request.Count,
                request.TopicId,
                request.GrammarId);

            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GenerateAndSaveConversation")
        .WithSummary("AI: Generate N conversation scripts with lines translated into TargetLanguageCodes, then save. Returns list.")
        .RequireAuthorization("curriculum.generate");

        // ── Exercise ───────────────────────────────────────────────────────

        admin.MapPost("/exercise", async (GenerateExerciseRequest request, ISender sender) =>
        {
            var command = new GenerateAndSaveExerciseCommand(
                request.Context,
                request.ExerciseType,
                request.Level,
                request.TopicId,
                request.VocabularyId,
                request.GrammarId,
                request.XpReward,
                request.Count);

            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GenerateAndSaveExercise")
        .WithSummary("AI: Generate N exercises (text-based types only), then save. Returns 400 for media-dependent types.")
        .RequireAuthorization("curriculum.generate");

        // ── Example Sentences ──────────────────────────────────────────────

        admin.MapPost("/example-sentences", async (GenerateExampleSentencesRequest request, ISender sender) =>
        {
            var command = new GenerateAndSaveExampleSentencesCommand(
                request.Context,
                request.Count,
                request.Level,
                request.TargetLanguageCodes,
                request.TopicId,
                request.VocabularyId,
                request.GrammarId);

            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GenerateAndSaveExampleSentences")
        .WithSummary("AI: Generate N example sentences with translations in TargetLanguageCodes, then save. Returns list.")
        .RequireAuthorization("curriculum.generate");
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record GenerateVocabularyRequest(
    string WordHint,
    int Count = 1,
    string? Level = null,
    Guid? TopicId = null,
    bool AutoGenerateExamples = false,
    int ExampleCount = 3,
    List<string>? ExampleTargetLanguageCodes = null);

public sealed record GenerateGrammarRequest(
    string Title,
    int Count = 1,
    string? Level = null,
    Guid? TopicId = null,
    int ExampleCount = 3,
    List<string>? TargetLanguageCodes = null);

public sealed record GenerateConversationRequest(
    string TopicName,
    int Count = 1,
    string? Level = null,
    string? Scenario = null,
    int LineCount = 6,
    List<string>? TargetLanguageCodes = null,
    Guid? TopicId = null,
    Guid? GrammarId = null);

public sealed record GenerateExerciseRequest(
    string Context,
    ExerciseType ExerciseType = ExerciseType.MultipleChoice,
    int Count = 1,
    string? Level = null,
    Guid? TopicId = null,
    Guid? VocabularyId = null,
    Guid? GrammarId = null,
    int XpReward = 10);

public sealed record GenerateExampleSentencesRequest(
    string Context,
    int Count = 3,
    string? Level = null,
    List<string>? TargetLanguageCodes = null,
    Guid? TopicId = null,
    Guid? VocabularyId = null,
    Guid? GrammarId = null);
