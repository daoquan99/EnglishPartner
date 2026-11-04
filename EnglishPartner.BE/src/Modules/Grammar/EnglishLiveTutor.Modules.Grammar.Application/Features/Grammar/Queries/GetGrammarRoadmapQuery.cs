using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Application.Modules.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries;

public sealed record GetGrammarRoadmapQuery : IQuery<GrammarRoadmapDto>;

public sealed class GetGrammarRoadmapQueryHandler : IQueryHandler<GetGrammarRoadmapQuery, GrammarRoadmapDto>
{
    private readonly IGrammarDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetGrammarRoadmapQueryHandler(IGrammarDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GrammarRoadmapDto> HandleAsync(GetGrammarRoadmapQuery query, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;

        // Query 1: All published, non-deleted steps without TheoryJson
        var steps = await _db.GrammarSteps
            .AsNoTracking()
            .Where(s => s.IsPublished && !s.IsDeleted)
            .Select(s => GrammarStepEntity.Create(
                s.Code,
                s.CefrLevel,
                s.Order,
                s.TitleVi,
                s.TitleEn,
                s.SummaryVi,
                "{}", // Omit TheoryJson to optimize performance
                s.TheoryVersion,
                s.IsLevelReview,
                s.IsPublished,
                s.Id))
            .ToListAsync(ct);

        // Query 2: Current user's progress records
        var userProgresses = string.IsNullOrWhiteSpace(userId)
            ? []
            : await _db.UserGrammarStepProgress
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToListAsync(ct);

        return GrammarRoadmapCalculator.ComputeRoadmap(steps, userProgresses);
    }
}
