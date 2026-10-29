using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries.ListMyGrammarExercises;

public sealed class ListMyGrammarExercisesQueryHandler
    : IQueryHandler<ListMyGrammarExercisesQuery, PagedResultDto<GrammarMyExerciseDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListMyGrammarExercisesQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResultDto<GrammarMyExerciseDto>> HandleAsync(
        ListMyGrammarExercisesQuery query,
        CancellationToken ct = default)
    {
        string userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem bài tập của mình.");

        int page = Math.Max(1, query.Page ?? 1);
        int pageSize = Math.Clamp(query.PageSize ?? PagedResultDto<GrammarMyExerciseDto>.DefaultPageSize, 1, PagedResultDto<GrammarMyExerciseDto>.MaxPageSize);

        var baseQuery = _db.GrammarExercises.AsNoTracking()
            .Where(e => e.OwnerUserId == userId && !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.StepCode))
        {
            string normCode = query.StepCode.Trim().ToUpperInvariant();
            var stepId = await _db.GrammarSteps.AsNoTracking()
                .Where(s => s.Code == normCode && !s.IsDeleted)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

            if (stepId.HasValue)
            {
                baseQuery = baseQuery.Where(e => e.StepId == stepId.Value);
            }
            else
            {
                return PagedResultDto<GrammarMyExerciseDto>.FromPage([], 0, query.Page, query.PageSize);
            }
        }

        if (query.Moderation.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Moderation == query.Moderation.Value);
        }

        int total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var stepIds = items.Select(e => e.StepId).Distinct().ToList();
        var stepCodeMap = await _db.GrammarSteps.AsNoTracking()
            .Where(s => stepIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Code, ct);

        var dtos = items.Select(e =>
        {
            string sCode = stepCodeMap.TryGetValue(e.StepId, out var c) ? c : string.Empty;
            return GrammarExerciseDtoMapper.MapToMyExerciseDto(e, sCode);
        }).ToList();

        return PagedResultDto<GrammarMyExerciseDto>.FromPage(dtos, total, query.Page, query.PageSize);
    }
}
