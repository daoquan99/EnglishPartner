namespace EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

public interface IPronunciationAssessmentObserver
{
    /// <summary>
    /// Records a pronunciation result for the authenticated learner.
    /// <paramref name="userId"/> must come from <c>ICurrentUser.UserId</c>, never from request payload data.
    /// </summary>
    Task OnAssessedAsync(
        string userId,
        Guid senseId,
        decimal overallScore,
        CancellationToken ct = default);
}
