using System.Text.Json;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.Grammar;

namespace EnglishLiveTutor.Application.Features.Grammar.Common;

public static class GrammarExerciseDtoMapper
{
    public static GrammarMyExerciseDto MapToMyExerciseDto(GrammarExerciseEntity e, string stepCode)
    {
        JsonElement contentElement;
        try
        {
            using var doc = JsonDocument.Parse(e.ContentJson);
            contentElement = doc.RootElement.Clone();
        }
        catch
        {
            using var fallback = JsonDocument.Parse("{}");
            contentElement = fallback.RootElement.Clone();
        }

        JsonElement? gradingElement = null;
        if (!string.IsNullOrWhiteSpace(e.GradingDefinitionJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(e.GradingDefinitionJson);
                gradingElement = doc.RootElement.Clone();
            }
            catch
            {
                gradingElement = null;
            }
        }

        return new GrammarMyExerciseDto(
            Id: e.Id,
            StepId: e.StepId,
            StepCode: stepCode,
            Key: e.Key,
            Type: e.Type,
            Difficulty: e.Difficulty,
            Title: e.Title,
            Instructions: e.Instructions,
            Focus: e.Focus,
            Content: contentElement,
            Grading: gradingElement,
            MaxPoints: e.MaxPoints,
            GradingMode: e.GradingMode,
            Source: e.Source,
            OwnerUserId: e.OwnerUserId,
            AuthorUserId: e.AuthorUserId,
            Visibility: e.Visibility,
            Moderation: e.Moderation,
            RejectReason: e.RejectReason,
            ReviewedBy: e.ReviewedBy,
            ReviewedAtUtc: e.ReviewedAtUtc,
            IsCore: e.IsCore,
            ContentHash: e.ContentHash,
            CreatedAtUtc: e.CreatedAtUtc,
            UpdatedAtUtc: e.UpdatedAtUtc
        );
    }

    public static GrammarModerationItemDto MapToModerationItemDto(
        GrammarExerciseEntity e,
        string stepCode,
        string cefrLevel,
        string? authorName = null)
    {
        JsonElement contentElement;
        try
        {
            using var doc = JsonDocument.Parse(e.ContentJson);
            contentElement = doc.RootElement.Clone();
        }
        catch
        {
            using var fallback = JsonDocument.Parse("{}");
            contentElement = fallback.RootElement.Clone();
        }

        JsonElement? gradingElement = null;
        if (!string.IsNullOrWhiteSpace(e.GradingDefinitionJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(e.GradingDefinitionJson);
                gradingElement = doc.RootElement.Clone();
            }
            catch
            {
                gradingElement = null;
            }
        }

        return new GrammarModerationItemDto(
            Id: e.Id,
            StepId: e.StepId,
            StepCode: stepCode,
            CefrLevel: cefrLevel,
            AuthorUserId: e.AuthorUserId,
            AuthorName: authorName,
            Type: e.Type,
            Difficulty: e.Difficulty,
            Title: e.Title,
            Instructions: e.Instructions,
            Focus: e.Focus,
            Content: contentElement,
            Grading: gradingElement,
            MaxPoints: e.MaxPoints,
            GradingMode: e.GradingMode,
            Visibility: e.Visibility,
            Moderation: e.Moderation,
            RejectReason: e.RejectReason,
            CreatedAtUtc: e.CreatedAtUtc
        );
    }
}
