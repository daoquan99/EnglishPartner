namespace EnglishLiveTutor.Application.Features.VocabularyGames.Exceptions;

public sealed class DailyPlanNotFoundException : Exception
{
    public DailyPlanNotFoundException(string message = "Chưa có kế hoạch Daily Vocabulary cho ngày hôm nay.")
        : base(message)
    {
    }
}
