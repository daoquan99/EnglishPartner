using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Profile.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(
    Guid UserId,
    string? DisplayName,
    string? NativeLanguage,
    string? TargetLanguage,
    string? ProficiencyLevel,
    string? LearningGoal,
    int? DailyGoalMinutes,
    string? AvatarUrl,
    DateTime? DateOfBirth,
    int? MorningReminderHour,
    int? EveningReminderHour,
    int? UtcOffsetHours) : ICommand;
