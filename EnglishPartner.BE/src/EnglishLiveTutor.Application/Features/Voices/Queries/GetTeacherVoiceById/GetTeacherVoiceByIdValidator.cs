using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Voices.Queries.GetTeacherVoiceById;

public sealed class GetTeacherVoiceByIdValidator : AbstractValidator<GetTeacherVoiceByIdQuery>
{
    public GetTeacherVoiceByIdValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
