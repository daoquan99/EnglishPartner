using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class OnboardingAnswerRepository(IdentityDbContext context) : IOnboardingAnswerRepository
{
    public async Task<List<OnboardingAnswer>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.OnboardingAnswers
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Order)
            .ToListAsync(cancellationToken);

    public void AddRange(IEnumerable<OnboardingAnswer> answers)
        => context.OnboardingAnswers.AddRange(answers);
}
