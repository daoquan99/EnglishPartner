using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningHistory;

public sealed class GetLearningHistoryValidator : AbstractValidator<GetLearningHistoryQuery>
{
    public GetLearningHistoryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
        RuleFor(x => x.Limit).GreaterThan(0).WithMessage("Limit phải lớn hơn 0.");
    }
}
