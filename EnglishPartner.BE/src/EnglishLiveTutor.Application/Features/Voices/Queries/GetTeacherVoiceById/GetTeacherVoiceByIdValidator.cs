using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Voices.Queries.GetTeacherVoiceById;

public sealed class GetTeacherVoiceByIdValidator : AbstractValidator<GetTeacherVoiceByIdQuery>
{
    public GetTeacherVoiceByIdValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
    }
}
