using System.Text.Json;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Common;

public static class GrammarAttemptDtoMapper
{
    public static async Task<GrammarAttemptDto> MapToDtoAsync(
        IAppDbContext db,
        IGrammarLearnerContentSanitizer sanitizer,
        GrammarAttemptEntity attempt,
        string? currentUserId,
        CancellationToken ct = default)
    {
        string? stepCode = null;
        if (attempt.StepId.HasValue)
        {
            stepCode = await db.GrammarSteps
                .AsNoTracking()
                .Where(s => s.Id == attempt.StepId.Value)
                .Select(s => s.Code)
                .FirstOrDefaultAsync(ct);
        }

        var exerciseIds = attempt.ExerciseIds;
        var exercises = await db.GrammarExercises
            .AsNoTracking()
            .Where(e => exerciseIds.Contains(e.Id))
            .ToListAsync(ct);

        var exerciseMap = exercises.ToDictionary(e => e.Id);
        var exerciseDtos = new List<GrammarExerciseLearnerDto>(exerciseIds.Count);

        for (int i = 0; i < exerciseIds.Count; i++)
        {
            var exId = exerciseIds[i];
            if (!exerciseMap.TryGetValue(exId, out var ex) || ex.IsDeleted || !ex.IsVisibleTo(currentUserId))
            {
                exerciseDtos.Add(new GrammarExerciseLearnerDto(
                    Id: exId,
                    Key: null,
                    Type: default,
                    Title: string.Empty,
                    Instructions: string.Empty,
                    Order: i + 1,
                    MaxPoints: 0,
                    GradingMode: default,
                    Content: null,
                    Difficulty: 0,
                    Focus: null,
                    Source: default,
                    Visibility: default,
                    Moderation: default,
                    AuthorName: null,
                    Unavailable: true
                ));
                continue;
            }

            string sanitizedContent = sanitizer.Sanitize(ex.Type, ex.ContentJson);
            JsonElement contentElement;
            try
            {
                using var doc = JsonDocument.Parse(sanitizedContent);
                contentElement = doc.RootElement.Clone();
            }
            catch
            {
                using var doc = JsonDocument.Parse("{}");
                contentElement = doc.RootElement.Clone();
            }

            exerciseDtos.Add(new GrammarExerciseLearnerDto(
                Id: ex.Id,
                Key: ex.Key,
                Type: ex.Type,
                Title: ex.Title,
                Instructions: ex.Instructions,
                Order: i + 1,
                MaxPoints: ex.MaxPoints,
                GradingMode: ex.GradingMode,
                Content: contentElement,
                Difficulty: ex.Difficulty,
                Focus: ex.Focus,
                Source: ex.Source,
                Visibility: ex.Visibility,
                Moderation: ex.Moderation,
                AuthorName: ex.Source == GrammarExerciseSource.Official ? "EnglishPartner" : null,
                Unavailable: false
            ));
        }

        var answerDtos = attempt.Answers.Select(a =>
        {
            JsonElement? feedbackElement = null;
            if (!string.IsNullOrWhiteSpace(a.FeedbackJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(a.FeedbackJson);
                    feedbackElement = doc.RootElement.Clone();
                }
                catch { }
            }

            JsonElement? payloadElement = null;
            if (!string.IsNullOrWhiteSpace(a.AnswerPayloadJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(a.AnswerPayloadJson);
                    payloadElement = doc.RootElement.Clone();
                }
                catch { }
            }

            return new GrammarAttemptAnswerDto(
                ExerciseId: a.ExerciseId,
                Status: a.Status,
                Score100: a.Score100,
                PointsEarned: a.PointsEarned,
                MaxPoints: a.MaxPoints,
                Feedback: feedbackElement,
                AnswerPayload: payloadElement
            );
        }).ToList();

        return new GrammarAttemptDto(
            Id: attempt.Id,
            Kind: attempt.Kind,
            StepCode: stepCode,
            CefrLevel: attempt.CefrLevel,
            StartedAtUtc: attempt.StartedAtUtc,
            CompletedAtUtc: attempt.CompletedAtUtc,
            Score100: attempt.Score100,
            Exercises: exerciseDtos,
            Answers: answerDtos
        );
    }
}
