using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Identity.Domain.Users.Entities;

public sealed class OnboardingAnswer : Entity
{
    private OnboardingAnswer() { }

    public static OnboardingAnswer Create(Guid userId, string questionKey, string answer, int order)
    {
        return new OnboardingAnswer
        {
            Id = Guid.NewGuid(), UserId = userId, QuestionKey = questionKey, Answer = answer, Order = order
        };
    }

    public Guid UserId { get; private set; }
    public string QuestionKey { get; private set; } = default!;
    public string Answer { get; private set; } = default!;
    public int Order { get; private set; }
}
