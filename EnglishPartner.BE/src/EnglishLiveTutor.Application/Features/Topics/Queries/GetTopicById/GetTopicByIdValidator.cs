using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Topics.Queries.GetTopicById;

public sealed class GetTopicByIdValidator : AbstractValidator<GetTopicByIdQuery>
{
    public GetTopicByIdValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
