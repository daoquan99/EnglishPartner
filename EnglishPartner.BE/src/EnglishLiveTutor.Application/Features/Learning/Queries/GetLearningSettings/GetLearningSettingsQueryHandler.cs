using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Learning;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningSettings;

public sealed class GetLearningSettingsQueryHandler : IQueryHandler<GetLearningSettingsQuery, UserLearningSettingsDto>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public GetLearningSettingsQueryHandler(IAppDbContext db, TimeProvider? timeProvider = null)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<UserLearningSettingsDto> HandleAsync(GetLearningSettingsQuery query, CancellationToken ct = default)
    {
        string userId = query.UserId;

        // 1. Resolve user profile timezone and preferred level for defaults
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id.ToString() == userId || u.Username == userId)
            .Select(u => new { u.TimeZoneId, u.PreferredCefrLevel })
            .FirstOrDefaultAsync(ct);

        string timeZoneId = string.IsNullOrWhiteSpace(user?.TimeZoneId) ? "Asia/Ho_Chi_Minh" : user.TimeZoneId;
        string defaultLevel = UserLearningSettingsEntity.NormalizeCefrLevel(user?.PreferredCefrLevel, "B1");

        // 2. Read persisted settings (Read-only, DO NOT write to DB)
        var settings = await _db.UserLearningSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        int target = settings?.DailyVocabularyTarget ?? 20;
        string level = settings is not null
            ? UserLearningSettingsEntity.NormalizeCefrLevel(settings.CefrLevel, "B1")
            : defaultLevel;
        bool isPersisted = settings is not null;

        // 3. Determine effective date: today if plan not initialized yet, tomorrow if plan already exists
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
            DailyVocabularyTarget: target,
            CefrLevel: level,
            IsPersisted: isPersisted,
            EffectiveFromLearningDate: effectiveDate,
            TimeZoneId: timeZoneId
        );
    }
}
