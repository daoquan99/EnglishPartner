namespace EnglishLiveTutor.Application.Common.Identity;

public interface ICurrentUser
{
    string? UserId { get; }
    bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId) && !string.Equals(UserId, "default-learner", StringComparison.OrdinalIgnoreCase) && !string.Equals(UserId, "system", StringComparison.OrdinalIgnoreCase);
    string UserIdOrSystem => string.IsNullOrWhiteSpace(UserId) ? "system" : UserId!;
    string EffectiveLearnerId => string.IsNullOrWhiteSpace(UserId) ? "default-learner" : UserId!.Trim();
}
