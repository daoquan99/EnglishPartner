using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Identity.Domain.Users.Entities;

public sealed class UserProfile : Entity
{
    private UserProfile() { }

    public static UserProfile Create(Guid userId)
    {
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetLanguage = "en",
            DailyGoalMinutes = 15
        };
    }

    public Guid UserId { get; private set; }
    public string? NativeLanguage { get; private set; }
    public string TargetLanguage { get; private set; } = "en";
    public string? ProficiencyLevel { get; private set; }
    public string? LearningGoal { get; private set; }
    public int DailyGoalMinutes { get; private set; } = 15;
    public string? AvatarUrl { get; private set; }
    public DateTime? DateOfBirth { get; private set; }

    public void Update(
        string? nativeLanguage, string? targetLanguage, string? proficiencyLevel,
        string? learningGoal, int? dailyGoalMinutes, string? avatarUrl, DateTime? dateOfBirth)
    {
        if (nativeLanguage is not null) NativeLanguage = nativeLanguage;
        if (targetLanguage is not null) TargetLanguage = targetLanguage;
        if (proficiencyLevel is not null) ProficiencyLevel = proficiencyLevel;
        if (learningGoal is not null) LearningGoal = learningGoal;
        if (dailyGoalMinutes.HasValue) DailyGoalMinutes = dailyGoalMinutes.Value;
        if (avatarUrl is not null) AvatarUrl = avatarUrl;
        if (dateOfBirth.HasValue) DateOfBirth = dateOfBirth.Value;
    }
}
