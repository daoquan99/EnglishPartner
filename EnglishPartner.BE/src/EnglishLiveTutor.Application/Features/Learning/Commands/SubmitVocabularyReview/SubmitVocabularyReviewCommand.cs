using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Domain.Learning;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.SubmitVocabularyReview;

public sealed record SubmitVocabularyReviewCommand(
    string UserId,
    Guid DailyItemId,
    SubmitVocabularyReviewRequest Request
) : ICommand<VocabularyFlashcardDto>;

public sealed class SubmitVocabularyReviewCommandHandler : ICommandHandler<SubmitVocabularyReviewCommand, VocabularyFlashcardDto>
{
    private readonly IAppDbContextFactory _dbFactory;

    public SubmitVocabularyReviewCommandHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<VocabularyFlashcardDto> HandleAsync(SubmitVocabularyReviewCommand command, CancellationToken ct = default)
    {
        var userId = command.UserId;
        var dailyItemId = command.DailyItemId;
        var req = command.Request;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var now = DateTimeOffset.UtcNow;

        var item = await db.DailyVocabularyItems
            .FirstOrDefaultAsync(i => i.Id == dailyItemId, ct);

        if (item is null)
            throw new InvalidOperationException("Không tìm thấy mục từ vựng trong kế hoạch học tập hôm nay.");

        var progress = await db.UserVocabularyProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SenseId == item.SenseId, ct);

        if (progress is null)
        {
            progress = new UserVocabularyProgressEntity(
                userId: userId,
                vocabularyId: item.VocabularyId,
                senseId: item.SenseId,
                masteryLevel: 0,
                intervalDays: 0,
                easeFactor: 2.50m,
                learningStatus: "New");
            db.UserVocabularyProgresses.Add(progress);
        }

        string rating = progress.ApplyReview(req.Rating, now);
        item.Complete(now);

        db.VocabularyReviewAttempts.Add(new VocabularyReviewAttemptEntity(
            userId: userId,
            vocabularyId: item.VocabularyId,
            senseId: item.SenseId,
            rating: rating,
            reviewedAt: now,
            nextReviewAtSnapshot: progress.NextReviewAt,
            reviewType: "FlashcardRecall"));

        await db.SaveChangesAsync(ct);

        // Check if all items in today's plan are completed
        bool anyRemaining = await db.DailyVocabularyItems
            .AnyAsync(i => i.PlanId == item.PlanId && i.Status != "Completed", ct);
        if (!anyRemaining)
        {
            var plan = await db.DailyVocabularyPlans.FirstOrDefaultAsync(p => p.Id == item.PlanId, ct);
            if (plan is not null)
            {
                plan.Complete(now);
                await db.SaveChangesAsync(ct);
            }
        }

        var updatedCard = await VocabularyProjectionHelper.GetFlashcardByVocabularyIdAsync(db, item.VocabularyId, item.SenseId, userId, ct);
        return updatedCard! with
        {
            DailyItemId = item.Id,
            ItemType = item.ItemType,
            ItemStatus = item.Status
        };
    }
}
