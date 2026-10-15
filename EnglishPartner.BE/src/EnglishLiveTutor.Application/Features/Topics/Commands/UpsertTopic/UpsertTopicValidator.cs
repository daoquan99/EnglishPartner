using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Topics.Commands.UpsertTopic;

public sealed class UpsertTopicValidator : AbstractValidator<UpsertTopicCommand>
{
    public UpsertTopicValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
