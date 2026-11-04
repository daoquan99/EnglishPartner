using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.GetUserProfile;

public sealed class GetUserProfileValidator : AbstractValidator<GetUserProfileQuery>
{
    public GetUserProfileValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
    }
}
