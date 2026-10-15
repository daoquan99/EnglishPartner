using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Topics.Commands.DeleteTopic;

public sealed class DeleteTopicValidator : AbstractValidator<DeleteTopicCommand>
{
    public DeleteTopicValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
