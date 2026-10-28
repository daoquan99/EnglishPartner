using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Topics.Queries.GetTopicById;

public sealed class GetTopicByIdValidator : AbstractValidator<GetTopicByIdQuery>
{
    public GetTopicByIdValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
    }
}
