using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Learning.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.InitializeDailyVocabularyPlan;

public sealed class InitializeDailyVocabularyPlanCommandHandler : ICommandHandler<InitializeDailyVocabularyPlanCommand, DailyVocabularyPlanResponseDto>
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly ISender _sender;

    public InitializeDailyVocabularyPlanCommandHandler(
        IAppDbContextFactory dbFactory,
        ISender sender)
    {
        _dbFactory = dbFactory;
        _sender = sender;
    }

    public async Task<DailyVocabularyPlanResponseDto> HandleAsync(InitializeDailyVocabularyPlanCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var userId = command.UserId;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        string learningDate = DailyVocabularyPlanEntity.ResolveLocalLearningDate(req.TimeZoneId);
        int targetNew = Math.Clamp(req.NewVocabularyTarget <= 0 ? 20 : req.NewVocabularyTarget, 1, 100);
        var now = DateTimeOffset.UtcNow;

        var plan = await db.DailyVocabularyPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LearningDate == learningDate, ct);

        if (plan is null)
        {
            plan = new DailyVocabularyPlanEntity(
                userId: userId,
                learningDate: learningDate,
                timeZoneId: string.IsNullOrWhiteSpace(req.TimeZoneId) ? "Asia/Ho_Chi_Minh" : req.TimeZoneId,
                newVocabularyTarget: targetNew,
                status: "InProgress");
            db.DailyVocabularyPlans.Add(plan);
            await db.SaveChangesAsync(ct);
        }
        else if (plan.NewVocabularyTarget != targetNew && req.NewVocabularyTarget > 0)
        {
            plan.UpdateTarget(targetNew);
            await db.SaveChangesAsync(ct);
        }

        var existingSenseIdsInPlan = plan.Items.Select(i => i.SenseId).ToHashSet();
        int nextOrder = plan.Items.Count == 0 ? 1 : plan.Items.Max(i => i.DisplayOrder) + 1;

        // 1. Carry-over unfinished items from earlier dates
        var unfinishedPastItems = await (
            from i in db.DailyVocabularyItems
            join p in db.DailyVocabularyPlans on i.PlanId equals p.Id
            where p.UserId == userId &&
                  p.LearningDate != learningDate &&
                  i.Status != "Completed"
            orderby i.AssignedAt
            select i
        ).ToListAsync(ct);

        foreach (var pastItem in unfinishedPastItems)
        {
            if (existingSenseIdsInPlan.Add(pastItem.SenseId))
            {
                db.DailyVocabularyItems.Add(new DailyVocabularyItemEntity(
                    planId: plan.Id,
                    vocabularyId: pastItem.VocabularyId,
                    senseId: pastItem.SenseId,
                    itemType: "CarryOver",
                    status: "Pending",
                    assignedAt: now,
                    displayOrder: nextOrder++));
            }
        }

        // 2. Pending due reviews (NextReviewAt <= now)
        var dueProgresses = await db.UserVocabularyProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.NextReviewAt != null && p.NextReviewAt <= now)
            .OrderBy(p => p.NextReviewAt)
            .Take(30)
            .ToListAsync(ct);

        foreach (var due in dueProgresses)
        {
            if (existingSenseIdsInPlan.Add(due.SenseId))
            {
                db.DailyVocabularyItems.Add(new DailyVocabularyItemEntity(
                    planId: plan.Id,
                    vocabularyId: due.VocabularyId,
                    senseId: due.SenseId,
                    itemType: "Review",
                    status: "Pending",
                    assignedAt: now,
                    displayOrder: nextOrder++));
            }
        }

        await db.SaveChangesAsync(ct);

        // Reload plan items to check how many New items are currently assigned
        var currentPlanItems = await db.DailyVocabularyItems
            .Where(i => i.PlanId == plan.Id)
            .ToListAsync(ct);
        existingSenseIdsInPlan = currentPlanItems.Select(i => i.SenseId).ToHashSet();

        int currentNewCount = currentPlanItems.Count(i => i.ItemType == "New");
        int neededNewCount = Math.Max(0, plan.NewVocabularyTarget - currentNewCount);

        if (neededNewCount > 0)
        {
            var learnedSenseIds = await db.UserVocabularyProgresses
                .Where(p => p.UserId == userId)
                .Select(p => p.SenseId)
                .ToListAsync(ct);
            var excludedSenses = existingSenseIdsInPlan.Union(learnedSenseIds).ToHashSet();

            var candidateSenses = await (
                from s in db.VocabularySenses.AsNoTracking()
                join v in db.Vocabularies.AsNoTracking() on s.VocabularyId equals v.Id
                where v.IsActive && !excludedSenses.Contains(s.Id)
                orderby v.FrequencyRank ?? 99999, s.DisplayOrder
                select s
            )
            .Take(neededNewCount)
            .ToListAsync(ct);

            foreach (var sense in candidateSenses)
            {
                if (existingSenseIdsInPlan.Add(sense.Id))
                {
                    db.DailyVocabularyItems.Add(new DailyVocabularyItemEntity(
                        planId: plan.Id,
                        vocabularyId: sense.VocabularyId,
                        senseId: sense.Id,
                        itemType: "New",
                        status: "Pending",
                        assignedAt: now,
                        displayOrder: nextOrder++));
                    neededNewCount--;
                }
            }

            await db.SaveChangesAsync(ct);
        }

        // 3. Generate additional vocabulary using AI when requested and API key is supplied
        if (req.GenerateWithAiIfNeeded && neededNewCount > 0 && !string.IsNullOrWhiteSpace(req.ApiKey))
        {
            int batchToGenerate = Math.Min(neededNewCount, 5);
            await _sender.Send(
                new GenerateVocabularyCommand(
                    userId,
                    new GenerateVocabularyRequest(
                        ApiKey: req.ApiKey!,
                        ModelCode: req.GenerationModelCode ?? "gemini-3.8-flash",
                        RequestedCount: batchToGenerate,
                        CefrLevel: req.CefrLevel,
                        Topic: req.Topic,
                        ExcludedWords: null,
                        AssignToTodayPlan: true
                    )
                ),
                ct);
        }

        return await VocabularyProjectionHelper.BuildDailyPlanResponseAsync(db, plan.Id, userId, ct);
    }
}
