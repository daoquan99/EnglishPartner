using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Topics.Commands.DeleteTopic;

public sealed class DeleteTopicValidator : AbstractValidator<DeleteTopicCommand>
{
    public DeleteTopicValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
    }
}
