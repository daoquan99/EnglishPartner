namespace EnglishLiveTutor.Infrastructure.Identity;

public sealed class DemoLearnerSeedOptions
{
    public const string SectionName = "DemoLearner";

    public string Username { get; set; } = "default-learner";
    public string Email { get; set; } = "learner@lingomint.local";
    public string? Password { get; set; }
    public string FullName { get; set; } = "Học viên Mặc định";
    public string PreferredCefrLevel { get; set; } = "B1";
    public string PreferredVoiceName { get; set; } = "Aoede";
}
