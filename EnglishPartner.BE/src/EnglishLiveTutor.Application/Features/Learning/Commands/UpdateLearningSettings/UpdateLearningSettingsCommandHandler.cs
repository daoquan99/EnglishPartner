using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Learning;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.UpdateLearningSettings;

public sealed class UpdateLearningSettingsCommandHandler : ICommandHandler<UpdateLearningSettingsCommand, UserLearningSettingsDto>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public UpdateLearningSettingsCommandHandler(IAppDbContext db, TimeProvider? timeProvider = null)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<UserLearningSettingsDto> HandleAsync(UpdateLearningSettingsCommand command, CancellationToken ct = default)
    {
        string userId = command.UserId;
        var req = command.Request;

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId không được để trống.", nameof(userId));

        // Domain entity constructor & Update method validate target (1..100) and cefrLevel (A1..C2)
        var settings = await _db.UserLearningSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (settings is null)
        {
            settings = new UserLearningSettingsEntity(
                userId: userId,
                dailyVocabularyTarget: req.DailyVocabularyTarget,
                cefrLevel: req.CefrLevel
            );
            _db.UserLearningSettings.Add(settings);
        }
        else
        {
            settings.Update(req.DailyVocabularyTarget, req.CefrLevel);
        }

        await _db.SaveChangesAsync(ct);

        // Resolve user timezone for effective date calculation
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id.ToString() == userId || u.Username == userId)
            .Select(u => new { u.TimeZoneId })
            .FirstOrDefaultAsync(ct);

        string timeZoneId = string.IsNullOrWhiteSpace(user?.TimeZoneId) ? "Asia/Ho_Chi_Minh" : user.TimeZoneId;
        string today = DailyVocabularyPlanEntity.ResolveLocalLearningDate(timeZoneId, _timeProvider);
        bool hasPlanToday = await _db.DailyVocabularyPlans
            .AsNoTracking()
            .AnyAsync(p => p.UserId == userId && p.LearningDate == today, ct);

        string effectiveDate;
        if (!hasPlanToday)
        {
            effectiveDate = today;
        }
        else
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var localNow = TimeZoneInfo.ConvertTime(now, tz);
                effectiveDate = localNow.AddDays(1).ToString("yyyy-MM-dd");
            }
            catch
            {
                effectiveDate = now.ToOffset(TimeSpan.FromHours(7)).AddDays(1).ToString("yyyy-MM-dd");
            }
        }

        return new UserLearningSettingsDto(
            DailyVocabularyTarget: settings.DailyVocabularyTarget,
            CefrLevel: settings.CefrLevel,
            IsPersisted: true,
            EffectiveFromLearningDate: effectiveDate,
            TimeZoneId: timeZoneId
        );
    }
}
