using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.ImportVocabularyJson;

public sealed class ImportVocabularyJsonValidator : AbstractValidator<ImportVocabularyJsonCommand>
{
    public ImportVocabularyJsonValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");

        When(x => x.Request != null && x.Request.ExamplesPerSense.HasValue, () =>
        {
            RuleFor(x => x.Request.ExamplesPerSense!.Value)
                .InclusiveBetween(1, 5)
                .WithMessage("Số câu ví dụ mỗi nghĩa phải từ 1 đến 5.");
        });
    }
}
