using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Domain.Learning;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.InitializeDailyVocabularyPlan;

public sealed class InitializeDailyVocabularyPlanCommandHandler : ICommandHandler<InitializeDailyVocabularyPlanCommand, DailyVocabularyPlanResponseDto>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public InitializeDailyVocabularyPlanCommandHandler(
        IAppDbContext db,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<DailyVocabularyPlanResponseDto> HandleAsync(InitializeDailyVocabularyPlanCommand command, CancellationToken ct = default)
    {
        string userId = command.UserId;
        var now = _timeProvider.GetUtcNow();

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId không được để trống.", nameof(userId));

        // 1. Resolve user profile for default timezone and cefr level
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id.ToString() == userId || u.Username == userId)
            .Select(u => new { u.TimeZoneId, u.PreferredCefrLevel })
            .FirstOrDefaultAsync(ct);

        string timeZoneId = string.IsNullOrWhiteSpace(user?.TimeZoneId) ? "Asia/Ho_Chi_Minh" : user.TimeZoneId;
        string defaultLevel = UserLearningSettingsEntity.NormalizeCefrLevel(user?.PreferredCefrLevel, "B1");

        // 2. Resolve or persist UserLearningSettings
        var settings = await _db.UserLearningSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        int target;
        string targetLevel;

        bool isNewSettings = false;
        if (settings is null)
        {
            target = 20;
            targetLevel = defaultLevel;
            settings = new UserLearningSettingsEntity(userId, target, targetLevel);
            _db.UserLearningSettings.Add(settings);
            isNewSettings = true;
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
            // If settings were newly staged, commit them
            if (isNewSettings)
            {
                await _db.SaveChangesAsync(ct);
            }

            return await VocabularyProjectionHelper.BuildDailyPlanResponseAsync(_db, existingPlan.Id, userId, ct);
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
        // Keep sense originally assigned, order by earliest assignment with stable tie-breaker
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

        int nextOrder = 1;
        var distinctCarryOverMap = new Dictionary<Guid, (Guid SenseId, DateTimeOffset AssignedAt)>();
        foreach (var past in pastAssignedItems)
        {
            if (learnedVocabSet.Contains(past.VocabularyId))
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
        int needed = Math.Max(0, target - carryOverCount);

        // 9. Catalog selection for needed new items
        if (needed > 0)
        {
            var excludedVocabIds = plan.Items.Select(i => i.VocabularyId).Union(learnedVocabSet).ToHashSet();

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
            // A sense must not appear twice in the same plan
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

        // 11. Atomic commit with concurrency collision resolution
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Another concurrent request created the plan for this user and date
            var winner = await _db.DailyVocabularyPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LearningDate == learningDate, ct);

            if (winner is not null)
            {
                return await VocabularyProjectionHelper.BuildDailyPlanResponseAsync(_db, winner.Id, userId, ct);
            }

            throw;
        }

        return await VocabularyProjectionHelper.BuildDailyPlanResponseAsync(_db, plan.Id, userId, ct);
    }
}
