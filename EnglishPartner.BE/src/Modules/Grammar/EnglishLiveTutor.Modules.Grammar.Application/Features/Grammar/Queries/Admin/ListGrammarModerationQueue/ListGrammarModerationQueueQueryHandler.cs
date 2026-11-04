using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Identity.Contracts;
using EnglishLiveTutor.Application.Modules.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries.Admin.ListGrammarModerationQueue;

public sealed class ListGrammarModerationQueueQueryHandler
    : IQueryHandler<ListGrammarModerationQueueQuery, PagedResultDto<GrammarModerationItemDto>>
{
    private readonly IGrammarDbContext _db;
    private readonly IUserDirectory _userDirectory;

    public ListGrammarModerationQueueQueryHandler(IGrammarDbContext db, IUserDirectory userDirectory)
    {
        _db = db;
        _userDirectory = userDirectory;
    }

    public async Task<PagedResultDto<GrammarModerationItemDto>> HandleAsync(
        ListGrammarModerationQueueQuery query,
        CancellationToken ct = default)
    {
        int page = Math.Max(1, query.Page ?? 1);
        int pageSize = Math.Clamp(query.PageSize ?? PagedResultDto<GrammarModerationItemDto>.DefaultPageSize, 1, PagedResultDto<GrammarModerationItemDto>.MaxPageSize);

        var baseQuery = _db.GrammarExercises.AsNoTracking()
            .Where(e => !e.IsDeleted &&
                        e.Source != GrammarExerciseSource.Official &&
                        e.Visibility == GrammarExerciseVisibility.Public &&
                        e.Moderation != GrammarModerationStatus.None &&
                        e.Moderation == query.Status);

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
                return PagedResultDto<GrammarModerationItemDto>.FromPage([], 0, query.Page, query.PageSize);
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Level))
        {
            string normLevel = query.Level.Trim().ToUpperInvariant();
            var stepIdsInLevel = await _db.GrammarSteps.AsNoTracking()
                .Where(s => s.CefrLevel == normLevel && !s.IsDeleted)
                .Select(s => s.Id)
                .ToListAsync(ct);

            baseQuery = baseQuery.Where(e => stepIdsInLevel.Contains(e.StepId));
        }

        int total = await baseQuery.CountAsync(ct);

        // Spec: sorted oldest first
        var items = await baseQuery
            .OrderBy(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var stepIds = items.Select(e => e.StepId).Distinct().ToList();
        var steps = await _db.GrammarSteps.AsNoTracking()
            .Where(s => stepIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => (s.Code, s.CefrLevel), ct);

        var authorIds = items
            .Select(e => e.AuthorUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct()
            .ToList();

        var authorNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (authorIds.Count > 0)
        {
            var users = await _userDirectory.ListUsersByIdOrUsernameAsync(authorIds, ct);

            foreach (var u in users)
            {
                string displayName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username;
                authorNameMap[u.Id.ToString()] = displayName;
                authorNameMap[u.Username] = displayName;
            }
        }

        var dtos = items.Select(e =>
        {
            string sCode = string.Empty;
            string sLevel = string.Empty;
            if (steps.TryGetValue(e.StepId, out var sInfo))
            {
                sCode = sInfo.Code;
                sLevel = sInfo.CefrLevel;
            }

            string? authorName = null;
            if (!string.IsNullOrWhiteSpace(e.AuthorUserId) &&
                authorNameMap.TryGetValue(e.AuthorUserId, out var name))
            {
                authorName = name;
            }

            return GrammarExerciseDtoMapper.MapToModerationItemDto(e, sCode, sLevel, authorName);
        }).ToList();

        return PagedResultDto<GrammarModerationItemDto>.FromPage(dtos, total, query.Page, query.PageSize);
    }
}
