using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Topics.Commands.UpsertTopic;

public sealed class UpsertTopicValidator : AbstractValidator<UpsertTopicCommand>
{
    public UpsertTopicValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
