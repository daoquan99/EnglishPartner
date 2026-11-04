namespace EnglishLiveTutor.Application.Features.Grammar;

public static class GrammarRules
{
    public const decimal StepPassThreshold = 70m;
    public const decimal LevelTestPassThreshold = 80m;
    public const int LevelTestMinItems = 20;
    public const int LevelTestMaxItems = 30;
    public const int PracticeMaxItems = 10;
    public const int MaxItemsPerRequest = 10;
    public const int MaxAdminItemsPerRequest = 20;
    public const int MaxDailyExercisesPerUser = 40;
    public const int MaxPendingExercisesPerUser = 30;
}
