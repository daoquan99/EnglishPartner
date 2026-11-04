using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Application.Modules.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries;

public sealed record GetGrammarStepQuery(string Code) : IQuery<GrammarStepDetailDto>;

public sealed class GetGrammarStepQueryHandler : IQueryHandler<GetGrammarStepQuery, GrammarStepDetailDto>
{
    private readonly IGrammarDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetGrammarStepQueryHandler(IGrammarDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GrammarStepDetailDto> HandleAsync(GetGrammarStepQuery query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query.Code))
        {
            throw new NotFoundException($"Grammar step code '{query.Code}' not found.");
        }

        string normalizedCode = query.Code.Trim().ToUpperInvariant();

        var step = await _db.GrammarSteps
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == normalizedCode && s.IsPublished && !s.IsDeleted, ct);

        if (step == null)
        {
            throw new NotFoundException($"Grammar step '{normalizedCode}' was not found or is not published.");
        }

        string? userId = _currentUser.UserId;

        // Fetch all published steps for ordering prev/next and computing unlock status
        var sortedSteps = await GrammarRoadmapCalculator.GetSortedPublishedStepsAsync(_db, ct);

        int stepIndex = sortedSteps.FindIndex(s => string.Equals(s.Code, normalizedCode, StringComparison.OrdinalIgnoreCase));
        string? prevCode = stepIndex > 0 ? sortedSteps[stepIndex - 1].Code : null;
        string? nextCode = (stepIndex >= 0 && stepIndex < sortedSteps.Count - 1) ? sortedSteps[stepIndex + 1].Code : null;

        var userProgresses = string.IsNullOrWhiteSpace(userId)
            ? []
            : await _db.UserGrammarStepProgress
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToListAsync(ct);

        var (status, bestScore100, _) = GrammarRoadmapCalculator.GetStepStatus(normalizedCode, sortedSteps, userProgresses);

        // Calculate exercise statistics with separate CountAsync queries for guaranteed translation
        var baseQuery = _db.GrammarExercises
            .AsNoTracking()
            .Where(e => e.StepId == step.Id && !e.IsDeleted);

        int coreCount = await baseQuery
            .CountAsync(e => e.Source == GrammarExerciseSource.Official && e.IsCore, ct);

        int communityCount = await baseQuery
            .CountAsync(e => (e.Source == GrammarExerciseSource.Official && !e.IsCore) ||
                             (e.Source != GrammarExerciseSource.Official && e.Moderation == GrammarModerationStatus.Approved), ct);

        int myCount = !string.IsNullOrWhiteSpace(userId)
            ? await baseQuery.CountAsync(e => e.OwnerUserId == userId, ct)
            : 0;

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

        return new GrammarStepDetailDto(
            Code: step.Code,
            CefrLevel: step.CefrLevel,
            Order: step.Order,
            TitleVi: step.TitleVi,
            TitleEn: step.TitleEn,
            SummaryVi: step.SummaryVi,
            IsLevelReview: step.IsLevelReview,
            Theory: theoryJsonElement,
            Status: status,
            BestScore100: bestScore100,
            CoreExerciseCount: coreCount,
            CommunityExerciseCount: communityCount,
            MyExerciseCount: myCount,
            PrevCode: prevCode,
            NextCode: nextCode
        );
    }
}
