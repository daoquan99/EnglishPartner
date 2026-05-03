using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Commands.UpdateTopic;

public sealed class UpdateTopicCommandValidator : AbstractValidator<UpdateTopicCommand>
{
    public UpdateTopicCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Level).MaximumLength(20);
    }
}
