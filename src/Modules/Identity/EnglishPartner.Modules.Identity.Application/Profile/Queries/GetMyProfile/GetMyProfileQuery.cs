using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Profile.Queries.GetMyProfile;

public sealed record GetMyProfileQuery(Guid UserId) : IQuery<MyProfileResponse>;

public sealed record MyProfileResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string? NativeLanguage,
    string TargetLanguage,
    string? ProficiencyLevel,
    string? LearningGoal,
    int DailyGoalMinutes,
    string? AvatarUrl,
    DateTime? DateOfBirth,
    int? MorningReminderHour,
    int? EveningReminderHour,
    int UtcOffsetHours,
    bool IsOnboardingCompleted);
