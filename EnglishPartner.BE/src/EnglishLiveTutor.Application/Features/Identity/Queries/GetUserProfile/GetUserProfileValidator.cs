using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.GetUserProfile;

public sealed class GetUserProfileValidator : AbstractValidator<GetUserProfileQuery>
{
    public GetUserProfileValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
