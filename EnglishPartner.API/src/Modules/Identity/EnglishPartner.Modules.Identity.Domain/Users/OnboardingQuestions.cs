namespace EnglishPartner.Modules.Identity.Domain.Users;

/// <summary>
/// Static catalog of onboarding questions. QuestionKey is the stable identifier stored in OnboardingAnswer.
/// </summary>
public static class OnboardingQuestions
{
    public sealed record QuestionDefinition(string Key, string Text, string[] Options, int Order);

    public static readonly IReadOnlyList<QuestionDefinition> All =
    [
        new("native_language",        "What is your native language?",                  ["vi", "en", "ja", "ko", "zh", "other"], 1),
        new("target_language",        "Which language do you want to learn?",           ["en", "ja", "ko", "zh", "other"],       2),
        new("proficiency_level",      "What is your current level?",                    ["Beginner", "Elementary", "Intermediate", "UpperIntermediate", "Advanced", "Mastery"], 3),
        new("learning_goal",          "What is your main learning goal?",               ["Communication", "Academic", "Business", "Travel", "ExamPrep", "Other"], 4),
        new("daily_goal",             "How many minutes per day do you want to learn?", ["5", "10", "15", "30", "60"], 5),
        new("morning_reminder_hour",  "When should we remind you in the morning?",      ["none", "6", "7", "8", "9", "10", "11", "12"], 6),
        new("evening_reminder_hour",  "When should we remind you in the evening?",      ["none", "18", "19", "20", "21", "22", "23", "24"], 7),
    ];
}
