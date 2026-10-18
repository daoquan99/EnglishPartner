using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Learning.Entities;
using EnglishLiveTutor.Domain.Learning.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.SubmitVocabularyReview;

public sealed class SubmitVocabularyReviewCommandHandler : ICommandHandler<SubmitVocabularyReviewCommand, VocabularyFlashcardDto>
{
    private readonly IDailyVocabularyPlanRepository _planRepo;
    private readonly IUserVocabularyProgressRepository _progressRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public SubmitVocabularyReviewCommandHandler(
        IDailyVocabularyPlanRepository planRepo,
        IUserVocabularyProgressRepository progressRepo,
        IUnitOfWork unitOfWork,
        IAppDbContext db,
        TimeProvider? timeProvider = null)
    {
        _planRepo = planRepo;
        _progressRepo = progressRepo;
        _unitOfWork = unitOfWork;
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<VocabularyFlashcardDto> HandleAsync(SubmitVocabularyReviewCommand command, CancellationToken ct = default)
    {
        var userId = command.UserId;
        var dailyItemId = command.DailyItemId;
        var req = command.Request;
        var now = _timeProvider.GetUtcNow();

        // 0. Check idempotency if ClientRequestId is provided
        if (!string.IsNullOrWhiteSpace(req.ClientRequestId))
        {
            var existingAttempt = await _db.VocabularyReviewAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ClientRequestId == req.ClientRequestId.Trim(), ct);

            if (existingAttempt is not null)
            {
                var cachedCard = await VocabularyProjectionHelper.GetFlashcardByVocabularyIdAsync(_db, existingAttempt.VocabularyId, existingAttempt.SenseId, userId, ct);
                if (cachedCard is not null)
                {
                    if (dailyItemId.HasValue && dailyItemId.Value != Guid.Empty)
                    {
                        var existingItem = await _db.DailyVocabularyItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == dailyItemId.Value, ct);
                        return cachedCard with
                        {
                            DailyItemId = dailyItemId.Value,
                            ItemType = existingItem?.ItemType ?? "New",
                            ItemStatus = existingItem?.Status ?? "Completed"
                        };
                    }
                    return cachedCard;
                }
            }
        }

        // 1. Verify plan and ownership
        DailyVocabularyPlanEntity? plan = null;
        DailyVocabularyItemEntity? item = null;

        if (dailyItemId.HasValue && dailyItemId.Value != Guid.Empty)
        {
            plan = await _planRepo.GetByItemIdWithItemsAsync(dailyItemId.Value, ct);
            if (plan is null || plan.UserId != userId)
            {
                throw new InvalidOperationException("Không tìm thấy mục từ vựng trong kế hoạch học tập của bạn.");
            }

            item = plan.Items.FirstOrDefault(i => i.Id == dailyItemId.Value);
        }
        else if (req.SenseId.HasValue && req.SenseId.Value != Guid.Empty)
        {
            plan = await _planRepo.GetByUserAndSenseIdWithItemsAsync(userId, req.SenseId.Value, ct);
            if (plan is not null && plan.UserId == userId)
            {
                item = plan.Items.FirstOrDefault(i => i.SenseId == req.SenseId.Value);
            }
        }

        Guid vocabularyId;
        Guid senseId;

        if (item is not null)
        {
            vocabularyId = item.VocabularyId;
            senseId = item.SenseId;
            plan?.CompleteItem(item.Id, now);
        }
        else if (req.SenseId.HasValue && req.SenseId.Value != Guid.Empty)
        {
            senseId = req.SenseId.Value;
            if (req.VocabularyId.HasValue && req.VocabularyId.Value != Guid.Empty)
            {
                vocabularyId = req.VocabularyId.Value;
            }
            else
            {
                vocabularyId = await _db.Vocabularies
                    .Where(v => v.Senses.Any(s => s.Id == senseId))
                    .Select(v => v.Id)
                    .FirstOrDefaultAsync(ct);

                if (vocabularyId == Guid.Empty)
                {
                    throw new InvalidOperationException("Không tìm thấy thông tin từ vựng tương ứng.");
                }
            }
        }
        else
        {
            throw new InvalidOperationException("Không tìm thấy mục từ vựng hoặc SenseId để thực hiện review.");
        }

        // 2. Load or create progress
        var progress = await _progressRepo.GetByUserAndSenseAsync(userId, senseId, ct);
        if (progress is null)
        {
            progress = new UserVocabularyProgressEntity(
                userId: userId,
                vocabularyId: vocabularyId,
                senseId: senseId,
                masteryLevel: 0,
                intervalDays: 0,
                easeFactor: 2.50m,
                learningStatus: "New");
            await _progressRepo.AddAsync(progress, ct);
        }

        // 3. Domain transitions
        string rating = progress.ApplyReview(req.Rating, now);

        // 4. Stage immutable attempt
        await _progressRepo.AddReviewAttemptAsync(new VocabularyReviewAttemptEntity(
            userId: userId,
            vocabularyId: vocabularyId,
            senseId: senseId,
            rating: rating,
            reviewedAt: now,
            nextReviewAtSnapshot: progress.NextReviewAt,
            reviewType: "FlashcardRecall",
            clientRequestId: string.IsNullOrWhiteSpace(req.ClientRequestId) ? null : req.ClientRequestId.Trim()), ct);

        // 5. Commit all staged changes atomically in ONE transaction
        await _unitOfWork.CommitChangesAsync(ct);

        // 6. Read back projected flashcard
        var updatedCard = await VocabularyProjectionHelper.GetFlashcardByVocabularyIdAsync(_db, vocabularyId, senseId, userId, ct);
        if (updatedCard is null)
        {
            throw new InvalidOperationException("Không tìm thấy thông tin từ vựng sau khi lưu đánh giá.");
        }

        if (item is not null)
        {
            return updatedCard with
            {
                DailyItemId = item.Id,
                ItemType = item.ItemType,
                ItemStatus = item.Status
            };
        }

        return updatedCard;
    }
}
