using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.SubmitVocabularyReview;

public sealed class SubmitVocabularyReviewValidator : AbstractValidator<SubmitVocabularyReviewCommand>
{
    public SubmitVocabularyReviewValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
        RuleFor(x => x.Request.Rating).NotEmpty().WithMessage("Rating không được để trống.");
        RuleFor(x => x)
            .Must(x => (x.DailyItemId.HasValue && x.DailyItemId.Value != Guid.Empty) ||
                       (x.Request != null && x.Request.SenseId.HasValue && x.Request.SenseId.Value != Guid.Empty))
            .WithMessage("Phải cung cấp DailyItemId hoặc SenseId để thực hiện review.");
    }
}
