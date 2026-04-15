using FluentValidation;

namespace EnglishPartner.Modules.Learning.Application.Admin.Commands.AdjustUserXp;

public sealed class AdjustUserXpCommandValidator : AbstractValidator<AdjustUserXpCommand>
{
    public AdjustUserXpCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Amount).NotEqual(0).WithMessage("Amount must be non-zero.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
    }
}
