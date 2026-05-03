using FluentValidation;

namespace EnglishPartner.Modules.Identity.Application.Profile.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(100)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.NativeLanguage)
            .MaximumLength(10)
            .When(x => x.NativeLanguage is not null);

        RuleFor(x => x.TargetLanguage)
            .MaximumLength(10)
            .When(x => x.TargetLanguage is not null);

        RuleFor(x => x.DailyGoalMinutes)
            .InclusiveBetween(5, 120)
            .When(x => x.DailyGoalMinutes.HasValue);
    }
}
