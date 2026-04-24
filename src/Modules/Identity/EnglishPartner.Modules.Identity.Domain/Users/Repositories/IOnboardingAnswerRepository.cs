using EnglishPartner.Modules.Identity.Domain.Users.Entities;

namespace EnglishPartner.Modules.Identity.Domain.Users.Repositories;

public interface IOnboardingAnswerRepository
{
    Task<List<OnboardingAnswer>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void AddRange(IEnumerable<OnboardingAnswer> answers);
}
