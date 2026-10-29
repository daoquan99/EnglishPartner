using EnglishLiveTutor.Application.Features.Vocabulary.Services;
using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;

public sealed class BuildVocabularyGenerationPromptValidator : AbstractValidator<BuildVocabularyGenerationPromptQuery>
{
    public BuildVocabularyGenerationPromptValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");

        When(x => x.Request != null, () =>
        {
            RuleFor(x => x.Request.ExamplesPerSense)
                .InclusiveBetween(1, 5)
                .WithMessage("Số câu ví dụ mỗi nghĩa phải từ 1 đến 5.");

            RuleFor(x => x.Request)
                .Must(r =>
                {
                    bool hasTerms = r.TargetTerms != null && r.TargetTerms.Count > 0;
                    bool hasSentence = !string.IsNullOrWhiteSpace(r.SourceSentence);
                    return !(hasTerms && hasSentence);
                })
                .WithMessage("Không thể cung cấp đồng thời danh sách từ chỉ định và câu gốc.");

            When(x => x.Request.TargetTerms != null && x.Request.TargetTerms.Count > 0, () =>
            {
                RuleFor(x => x.Request.TargetTerms!)
                    .Must(terms => terms.Count >= 1 && terms.Count <= 10)
                    .WithMessage("Danh sách từ chỉ định phải từ 1 đến 10 mục.")
                    .Must(terms =>
                    {
                        var normalized = terms.Select(TargetedVocabularyHelper.NormalizeTerm).ToList();
                        return normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() == terms.Count;
                    })
                    .WithMessage("Danh sách từ chỉ định không được chứa các từ trùng lặp.");

                RuleForEach(x => x.Request.TargetTerms!)
                    .Must(term =>
                    {
                        var norm = TargetedVocabularyHelper.NormalizeTerm(term);
                        return norm.Length >= 1 && norm.Length <= 80;
                    })
                    .WithMessage("Mỗi từ hoặc cụm từ chỉ định phải từ 1 đến 80 ký tự.")
                    .Must(term =>
                    {
                        var norm = TargetedVocabularyHelper.NormalizeTerm(term);
                        return TargetedVocabularyHelper.CountWords(norm) <= 6;
                    })
                    .WithMessage("Mỗi từ hoặc cụm từ chỉ định không được vượt quá 6 từ.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Request.SourceSentence), () =>
            {
                RuleFor(x => x.Request.SourceSentence!)
                    .Must(s =>
                    {
                        var trimmed = s.Trim();
                        return trimmed.Length >= 1 && trimmed.Length <= 500;
                    })
                    .WithMessage("Câu gốc phải từ 1 đến 500 ký tự.");

                RuleFor(x => x.Request.RequestedCount)
                    .InclusiveBetween(1, 5)
                    .WithMessage("Khi trích từ vựng từ câu gốc, số lượng từ yêu cầu phải từ 1 đến 5.");
            });

            When(x => (x.Request.TargetTerms == null || x.Request.TargetTerms.Count == 0) &&
                      string.IsNullOrWhiteSpace(x.Request.SourceSentence), () =>
            {
                RuleFor(x => x.Request.RequestedCount)
                    .InclusiveBetween(1, VocabularyGenerationDefaults.MaxCount)
                    .WithMessage($"Số lượng từ vựng yêu cầu phải từ 1 đến {VocabularyGenerationDefaults.MaxCount}.");
            });
        });
    }
}
