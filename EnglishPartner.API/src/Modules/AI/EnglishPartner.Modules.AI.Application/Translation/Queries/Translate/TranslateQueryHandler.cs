using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Application.Translation.Queries.Translate;

internal sealed class TranslateQueryHandler(
    IGeminiService geminiService) : IQueryHandler<TranslateQuery, string>
{
    public async Task<Result<string>> Handle(TranslateQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return Result.Success(string.Empty);

        if (string.IsNullOrWhiteSpace(request.TargetLanguageCode))
            return Result.Failure<string>(new Error("Ai.Translation.InvalidLanguage", "Target language code is required."));

        var result = await geminiService.TranslateAsync(request.Text, request.TargetLanguageCode, cancellationToken);
        return Result.Success(result);
    }
}
