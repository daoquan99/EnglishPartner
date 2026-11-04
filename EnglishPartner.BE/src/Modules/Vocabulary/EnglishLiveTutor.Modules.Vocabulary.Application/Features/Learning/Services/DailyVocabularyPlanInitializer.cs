using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Identity.Contracts;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Domain.Learning;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Services;

public sealed class DailyVocabularyPlanInitializer : IDailyVocabularyPlanInitializer
{
    private readonly IVocabularyDbContext _db;
    private readonly IUserDirectory _userDirectory;
    private readonly IPronunciationScoreReader _scoreReader;
    private readonly TimeProvider _timeProvider;

    public DailyVocabularyPlanInitializer(
        IVocabularyDbContext db,
        IUserDirectory userDirectory,
        IPronunciationScoreReader scoreReader,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _userDirectory = userDirectory;
        _scoreReader = scoreReader;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<DailyVocabularyPlanResponseDto> InitializeOrGetPlanAsync(
        string userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId không được để trống.", nameof(userId));

        var (plan, isNewlyCreated) = await PrepareTodayPlanAsync(userId, reservedQuota: 0, ct: ct);

        if (isNewlyCreated)
        {
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Another concurrent request created the plan for this user and date
                var winner = await _db.DailyVocabularyPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.LearningDate == plan.LearningDate, ct);

                if (winner is not null)
                {
                    return await VocabularyProjectionHelper.BuildDailyPlanResponseAsync(_db, _scoreReader, winner.Id, userId, ct);
                }

                throw;
            }
        }
        else
        {
            await _db.SaveChangesAsync(ct);
        }

        return await VocabularyProjectionHelper.BuildDailyPlanResponseAsync(_db, _scoreReader, plan.Id, userId, ct);
    }

    public async Task<(DailyVocabularyPlanEntity Plan, bool IsNewlyCreated)> PrepareTodayPlanAsync(
        string userId,
        int reservedQuota = 0,
        IEnumerable<Guid>? excludedVocabularyIds = null,
        IEnumerable<Guid>? excludedSenseIds = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId không được để trống.", nameof(userId));

        var now = _timeProvider.GetUtcNow();

        // 1. Resolve user profile for default timezone and cefr level
        var user = await _userDirectory.GetLearningProfileAsync(userId, ct);

        string timeZoneId = string.IsNullOrWhiteSpace(user?.TimeZoneId) ? "Asia/Ho_Chi_Minh" : user.TimeZoneId;
        string defaultLevel = UserLearningSettingsEntity.NormalizeCefrLevel(user?.PreferredCefrLevel, "B1");

        // 2. Resolve or persist UserLearningSettings
        var settings = await _db.UserLearningSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        int target;
        string targetLevel;

        if (settings is null)
        {
            target = 20;
            targetLevel = defaultLevel;
            settings = new UserLearningSettingsEntity(userId, target, targetLevel);
            _db.UserLearningSettings.Add(settings);
        }
        else
        {
            target = settings.DailyVocabularyTarget;
            targetLevel = UserLearningSettingsEntity.NormalizeCefrLevel(settings.CefrLevel, defaultLevel);
        }

        // 3. Resolve local learning date based on user's timezone policy
        string learningDate = DailyVocabularyPlanEntity.ResolveLocalLearningDate(timeZoneId, _timeProvider);

        // 4. Check if plan already exists for today -> Idempotency: return existing plan untouched!
        var existingPlan = await _db.DailyVocabularyPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LearningDate == learningDate, ct);

        if (existingPlan is not null)
        {
            return (existingPlan, false);
        }

        // 5. Create new plan snapshot
        var plan = new DailyVocabularyPlanEntity(
            userId: userId,
            learningDate: learningDate,
            timeZoneId: timeZoneId,
            newVocabularyTarget: target,
            targetCefrLevel: targetLevel,
            status: "InProgress");

        _db.DailyVocabularyPlans.Add(plan);

        // 6. Identify learned vocabularies (Review evidence: ReviewCount > 0)
        var learnedVocabIds = await _db.UserVocabularyProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.ReviewCount > 0)
            .Select(p => p.VocabularyId)
            .Distinct()
            .ToListAsync(ct);
        var learnedVocabSet = learnedVocabIds.ToHashSet();

        // 7. Carry-over unfinished items from past dates (learningDate < today), deduplicated by VocabularyId
        var pastAssignedItems = await (
            from i in _db.DailyVocabularyItems.AsNoTracking()
            join p in _db.DailyVocabularyPlans.AsNoTracking() on i.PlanId equals p.Id
            join v in _db.Vocabularies.AsNoTracking() on i.VocabularyId equals v.Id
            where p.UserId == userId &&
                  string.Compare(p.LearningDate, learningDate) < 0 &&
                  v.IsActive && !v.IsDeleted
            orderby i.AssignedAt, i.Id
            select new
            {
                i.VocabularyId,
                i.SenseId,
                i.AssignedAt
            }
        ).ToListAsync(ct);

        var excludedVocabSet = excludedVocabularyIds != null ? new HashSet<Guid>(excludedVocabularyIds) : null;
        var excludedSenseSet = excludedSenseIds != null ? new HashSet<Guid>(excludedSenseIds) : null;

        int nextOrder = 1;
        var distinctCarryOverMap = new Dictionary<Guid, (Guid SenseId, DateTimeOffset AssignedAt)>();
        foreach (var past in pastAssignedItems)
        {
            if (learnedVocabSet.Contains(past.VocabularyId))
                continue;

            if (excludedVocabSet != null && excludedVocabSet.Contains(past.VocabularyId))
                continue;

            if (excludedSenseSet != null && excludedSenseSet.Contains(past.SenseId))
                continue;

            if (!distinctCarryOverMap.ContainsKey(past.VocabularyId))
            {
                distinctCarryOverMap[past.VocabularyId] = (past.SenseId, past.AssignedAt);
            }
        }

        foreach (var (vocabId, (senseId, assignedAt)) in distinctCarryOverMap)
        {
            plan.AddItem(
                vocabularyId: vocabId,
                senseId: senseId,
                itemType: "CarryOver",
                displayOrder: nextOrder++,
                assignedAt: assignedAt);
        }

        // 8. Quota calculation for first learning items
        int carryOverCount = plan.Items.Count(i => i.ItemType == "CarryOver");
        int needed = Math.Max(0, target - carryOverCount - Math.Max(0, reservedQuota));

        // 9. Catalog selection for needed new items
        if (needed > 0)
        {
            var excludedVocabIds = plan.Items.Select(i => i.VocabularyId).Union(learnedVocabSet).ToHashSet();
            if (excludedVocabSet != null)
            {
                excludedVocabIds.UnionWith(excludedVocabSet);
            }

            var candidateSenses = await (
                from s in _db.VocabularySenses.AsNoTracking()
                join v in _db.Vocabularies.AsNoTracking() on s.VocabularyId equals v.Id
                where v.IsActive && !v.IsDeleted && s.CefrLevel == targetLevel && !excludedVocabIds.Contains(v.Id)
                select new
                {
                    VocabularyId = v.Id,
                    SenseId = s.Id,
                    FrequencyRank = v.FrequencyRank ?? 99999,
                    s.IsPrimary,
                    s.DisplayOrder
                }
            ).ToListAsync(ct);

            var chosenCandidates = candidateSenses
                .Where(s => excludedSenseSet == null || !excludedSenseSet.Contains(s.SenseId))
                .GroupBy(s => s.VocabularyId)
                .Select(g => g.OrderByDescending(s => s.IsPrimary).ThenBy(s => s.DisplayOrder).ThenBy(s => s.SenseId).First())
                .OrderBy(s => s.FrequencyRank)
                .ThenBy(s => s.VocabularyId)
                .Take(needed)
                .ToList();

            foreach (var candidate in chosenCandidates)
            {
                plan.AddItem(
                    vocabularyId: candidate.VocabularyId,
                    senseId: candidate.SenseId,
                    itemType: "New",
                    displayOrder: nextOrder++,
                    assignedAt: now);
            }
        }

        // 10. Due reviews (Pending reviews with NextReviewAt <= now, limit 30, separate from first learning quota)
        var dueProgresses = await _db.UserVocabularyProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.NextReviewAt != null && p.NextReviewAt <= now)
            .OrderBy(p => p.NextReviewAt)
            .Take(30)
            .ToListAsync(ct);

        var planSenseIds = plan.Items.Select(i => i.SenseId).ToHashSet();

        foreach (var due in dueProgresses)
        {
            if (planSenseIds.Add(due.SenseId))
            {
                plan.AddItem(
                    vocabularyId: due.VocabularyId,
                    senseId: due.SenseId,
                    itemType: "Review",
                    displayOrder: nextOrder++,
                    assignedAt: now);
            }
        }

        return (plan, true);
    }
}
