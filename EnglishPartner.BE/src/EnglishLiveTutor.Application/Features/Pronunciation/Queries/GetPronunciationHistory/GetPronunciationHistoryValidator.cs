using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationHistory;

public sealed class GetPronunciationHistoryValidator : AbstractValidator<GetPronunciationHistoryQuery>
{
    public GetPronunciationHistoryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
    }
}
