using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Progress.Queries.GetProgressOverview;

public sealed class GetProgressOverviewQueryValidator : AbstractValidator<GetProgressOverviewQuery>
{
    public GetProgressOverviewQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(x => x.Days)
            .InclusiveBetween(7, 90)
            .WithMessage("Days must be between 7 and 90.");
    }
}
