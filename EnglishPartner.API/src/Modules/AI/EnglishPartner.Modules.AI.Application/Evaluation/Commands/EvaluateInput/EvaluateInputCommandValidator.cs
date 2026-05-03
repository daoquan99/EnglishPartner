using FluentValidation;

namespace EnglishPartner.Modules.AI.Application.Evaluation.Commands.EvaluateInput;

internal sealed class EvaluateInputCommandValidator : AbstractValidator<EvaluateInputCommand>
{
    public EvaluateInputCommandValidator()
    {
        RuleFor(x => x.UserInput)
            .NotEmpty().WithMessage("User input is required.")
            .MaximumLength(1000);

        // Must provide either (ItemId + ItemType) or ItemContext
        RuleFor(x => x)
            .Must(x => x.ItemId.HasValue && x.ItemType.HasValue || !string.IsNullOrWhiteSpace(x.ItemContext))
            .WithName("Context")
            .WithMessage("Provide either (ItemId + ItemType) for server-side context, or ItemContext string.");

        // If ItemId is provided, ItemType is required
        When(x => x.ItemId.HasValue, () =>
        {
            RuleFor(x => x.ItemType)
                .NotNull().WithMessage("ItemType is required when ItemId is provided.");
        });

        RuleFor(x => x.ItemContext)
            .MaximumLength(2000)
            .When(x => x.ItemContext is not null);
    }
}
