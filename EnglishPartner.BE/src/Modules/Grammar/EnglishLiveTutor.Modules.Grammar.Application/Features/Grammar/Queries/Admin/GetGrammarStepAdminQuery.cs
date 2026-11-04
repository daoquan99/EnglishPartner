using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Application.Modules.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries.Admin;

public sealed record GetGrammarStepAdminQuery(string Code) : IQuery<GrammarStepAdminDetailDto>;

public sealed class GetGrammarStepAdminQueryHandler : IQueryHandler<GetGrammarStepAdminQuery, GrammarStepAdminDetailDto>
{
    private readonly IGrammarDbContext _db;

    public GetGrammarStepAdminQueryHandler(IGrammarDbContext db)
    {
        _db = db;
    }

    public async Task<GrammarStepAdminDetailDto> HandleAsync(GetGrammarStepAdminQuery query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query.Code))
        {
            throw new NotFoundException($"Grammar step code '{query.Code}' not found.");
        }

        string normalizedCode = query.Code.Trim().ToUpperInvariant();

        var step = await _db.GrammarSteps
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == normalizedCode && !s.IsDeleted, ct);

        if (step == null)
        {
            throw new NotFoundException($"Grammar step '{normalizedCode}' was not found.");
        }

        JsonElement theoryJsonElement;
        try
        {
            using var doc = JsonDocument.Parse(step.TheoryJson);
            theoryJsonElement = doc.RootElement.Clone();
        }
        catch
        {
            using var doc = JsonDocument.Parse("{}");
            theoryJsonElement = doc.RootElement.Clone();
        }

        var exercises = await _db.GrammarExercises
            .AsNoTracking()
            .Where(e => e.StepId == step.Id && !e.IsDeleted && e.Source == GrammarExerciseSource.Official)
            .OrderBy(e => e.Difficulty)
            .ThenBy(e => e.Key)
            .ToListAsync(ct);

        var exerciseDtos = exercises.Select(e =>
        {
            JsonElement contentElement;
            try
            {
                using var doc = JsonDocument.Parse(e.ContentJson);
                contentElement = doc.RootElement.Clone();
            }
            catch
            {
                using var doc = JsonDocument.Parse("{}");
                contentElement = doc.RootElement.Clone();
            }

            JsonElement? gradingElement = null;
            if (!string.IsNullOrWhiteSpace(e.GradingDefinitionJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(e.GradingDefinitionJson);
                    gradingElement = doc.RootElement.Clone();
                }
                catch { }
            }

            return new GrammarExerciseAdminDto(
                Id: e.Id,
                StepId: e.StepId,
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
        }).ToList();

        return new GrammarStepAdminDetailDto(
            Id: step.Id,
            Code: step.Code,
            CefrLevel: step.CefrLevel,
            Order: step.Order,
            TitleVi: step.TitleVi,
            TitleEn: step.TitleEn,
            SummaryVi: step.SummaryVi,
            IsLevelReview: step.IsLevelReview,
            IsPublished: step.IsPublished,
            TheoryEditedByAdmin: step.TheoryEditedByAdmin,
            TheoryVersion: step.TheoryVersion,
            Theory: theoryJsonElement,
            OfficialExercises: exerciseDtos,
            CreatedAtUtc: step.CreatedAtUtc,
            UpdatedAtUtc: step.UpdatedAtUtc
        );
    }
}
