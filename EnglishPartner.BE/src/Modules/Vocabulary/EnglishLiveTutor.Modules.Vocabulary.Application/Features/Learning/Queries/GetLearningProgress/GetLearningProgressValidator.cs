using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningProgress;

public sealed class GetLearningProgressValidator : AbstractValidator<GetLearningProgressQuery>
{
    public GetLearningProgressValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
    }
}
