using FluentValidation;

namespace EnglishPartner.Modules.Learning.Application.ItemMasteries.Commands.ReviewItem;

internal sealed class ReviewItemCommandValidator : AbstractValidator<ReviewItemCommand>
{
    public ReviewItemCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty();

        RuleFor(x => x.QualityRating)
            .InclusiveBetween(0, 5)
            .WithMessage("Quality rating must be between 0 (blackout) and 5 (perfect recall).");
    }
}
