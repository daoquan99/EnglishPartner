using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Commands.CreateTopic;

public sealed class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
{
    public CreateTopicCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Level).MaximumLength(20);
    }
}
