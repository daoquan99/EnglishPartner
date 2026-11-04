using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Application.Features.Identity.Contracts;
using EnglishLiveTutor.Application.Modules.Grammar;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries;

public sealed record ListGrammarExercisesQuery(
    string Code,
    string? Source = null,
    int? Page = null,
    int? PageSize = null,
    bool? Paged = null
) : IQuery<PagedResultDto<GrammarExerciseLearnerDto>>;

public sealed class ListGrammarExercisesQueryHandler : IQueryHandler<ListGrammarExercisesQuery, PagedResultDto<GrammarExerciseLearnerDto>>
{
    private readonly IGrammarDbContext _db;
    private readonly IUserDirectory _userDirectory;
    private readonly ICurrentUser _currentUser;
    private readonly IGrammarLearnerContentSanitizer _sanitizer;

    public ListGrammarExercisesQueryHandler(
        IGrammarDbContext db,
        IUserDirectory userDirectory,
        ICurrentUser currentUser,
        IGrammarLearnerContentSanitizer sanitizer)
    {
        _db = db;
        _userDirectory = userDirectory;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
    }

    public async Task<PagedResultDto<GrammarExerciseLearnerDto>> HandleAsync(ListGrammarExercisesQuery query, CancellationToken ct = default)
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
        string source = (query.Source ?? "core").Trim().ToLowerInvariant();

        IQueryable<GrammarExerciseEntity> exercisesQuery = _db.GrammarExercises
            .AsNoTracking()
            .Where(e => e.StepId == step.Id && !e.IsDeleted);

        switch (source)
        {
            case "core":
                exercisesQuery = exercisesQuery.Where(e => e.Source == GrammarExerciseSource.Official && e.IsCore);
                break;

            case "community":
                exercisesQuery = exercisesQuery.Where(e =>
                    (e.Source == GrammarExerciseSource.Official && !e.IsCore) ||
                    (e.Source != GrammarExerciseSource.Official && e.Moderation == GrammarModerationStatus.Approved));
                break;

            case "mine":
                if (string.IsNullOrWhiteSpace(userId))
                {
                    exercisesQuery = exercisesQuery.Where(_ => false);
                }
                else
                {
                    exercisesQuery = exercisesQuery.Where(e => e.OwnerUserId == userId);
                }
                break;

            default:
                throw new ArgumentException($"Invalid source '{source}'. Must be 'core', 'community', or 'mine'.", nameof(query.Source));
        }

        exercisesQuery = exercisesQuery
            .OrderBy(e => e.Difficulty)
            .ThenBy(e => e.Key)
            .ThenBy(e => e.CreatedAtUtc);

        int totalCount = await exercisesQuery.CountAsync(ct);
        int effPage = Math.Max(1, query.Page ?? 1);
        int effSize = Math.Clamp(query.PageSize ?? 20, 1, 100);

        var pagedEntities = await exercisesQuery
            .Skip((effPage - 1) * effSize)
            .Take(effSize)
            .ToListAsync(ct);

        // Fetch author names if any user-owned exercises
        var authorUserIds = pagedEntities
            .Where(e => !string.IsNullOrWhiteSpace(e.AuthorUserId))
            .Select(e => e.AuthorUserId!)
            .Distinct()
            .ToList();

        var authorNames = new Dictionary<string, string>();
        if (authorUserIds.Count > 0)
        {
            var authorGuids = new List<Guid>();
            var rawGuidMap = new Dictionary<Guid, string>();
            var nonGuidUsernames = new List<string>();

            foreach (var idStr in authorUserIds)
            {
                if (Guid.TryParse(idStr, out var parsedGuid))
                {
                    authorGuids.Add(parsedGuid);
                    rawGuidMap[parsedGuid] = idStr;
                }
                else
                {
                    nonGuidUsernames.Add(idStr);
                }
            }

            if (authorGuids.Count > 0)
            {
                var users = await _userDirectory.ListUsersByIdsAsync(authorGuids, ct);

                foreach (var u in users)
                {
                    if (rawGuidMap.TryGetValue(u.Id, out var origStr))
                    {
                        authorNames[origStr] = u.FullName ?? u.Username;
                    }
                }
            }

            if (nonGuidUsernames.Count > 0)
            {
                var users = await _userDirectory.ListUsersByUsernamesAsync(nonGuidUsernames, ct);

                foreach (var u in users)
                {
                    authorNames[u.Username] = u.FullName ?? u.Username;
                }
            }
        }

        var items = new List<GrammarExerciseLearnerDto>(pagedEntities.Count);
        for (int i = 0; i < pagedEntities.Count; i++)
        {
            var e = pagedEntities[i];
            string sanitizedContentJson = _sanitizer.Sanitize(e.Type, e.ContentJson);

            JsonElement contentElement;
            try
            {
                using var doc = JsonDocument.Parse(sanitizedContentJson);
                contentElement = doc.RootElement.Clone();
            }
            catch
            {
                using var doc = JsonDocument.Parse("{}");
                contentElement = doc.RootElement.Clone();
            }

            string? authorName = null;
            if (e.Source == GrammarExerciseSource.Official)
            {
                authorName = "EnglishPartner";
            }
            else if (!string.IsNullOrWhiteSpace(e.AuthorUserId) && authorNames.TryGetValue(e.AuthorUserId, out var name))
            {
                authorName = name;
            }

            items.Add(new GrammarExerciseLearnerDto(
                Id: e.Id,
                Key: e.Key,
                Type: e.Type,
                Title: e.Title,
                Instructions: e.Instructions,
                Order: (effPage - 1) * effSize + i + 1,
                MaxPoints: e.MaxPoints,
                GradingMode: e.GradingMode,
                Content: contentElement,
                Difficulty: e.Difficulty,
                Focus: e.Focus,
                Source: e.Source,
                Visibility: e.Visibility,
                Moderation: e.Moderation,
                AuthorName: authorName,
                Unavailable: false
            ));
        }

        return PagedResultDto<GrammarExerciseLearnerDto>.FromPage(items, totalCount, effPage, effSize);
    }
}
