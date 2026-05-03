using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Domain;
using EnglishPartner.Modules.Curriculum.Contracts.Services;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Application.Evaluation.Commands.EvaluateInput;

internal sealed class EvaluateInputCommandHandler(
    IGeminiService geminiService,
    ICurriculumQueryService curriculumQueryService) : ICommandHandler<EvaluateInputCommand, EvaluateInputResponse>
{
    public async Task<Result<EvaluateInputResponse>> Handle(EvaluateInputCommand request, CancellationToken cancellationToken)
    {
        // ── Resolve context ──
        string itemContext;

        if (request.ItemId.HasValue && request.ItemType.HasValue)
        {
            // Mode 1: Server-side — fetch context from Curriculum module
            var contextDto = await curriculumQueryService.GetItemContextAsync(
                request.ItemId.Value, request.ItemType.Value, cancellationToken);

            if (contextDto is null)
                return Result.Failure<EvaluateInputResponse>(
                    AiErrors.Evaluation.ItemNotFound(request.ItemId, request.ItemType));

            itemContext = BuildContextString(contextDto);
        }
        else if (!string.IsNullOrWhiteSpace(request.ItemContext))
        {
            // Mode 2: Client-provided context (legacy/standalone)
            itemContext = request.ItemContext;
        }
        else
        {
            return Result.Failure<EvaluateInputResponse>(AiErrors.Evaluation.MissingContext);
        }

        // ── Evaluate via Gemini ──
        var result = await geminiService.EvaluateInputAsync(itemContext, request.UserInput, cancellationToken);

        return new EvaluateInputResponse(
            result.Score,
            result.IsCorrect,
            result.SuggestedCorrection,
            result.Explanation);
    }

    private static string BuildContextString(CurriculumItemContextDto dto) =>
        $"[{dto.ItemType}] {dto.Question}" +
        (dto.Hint is not null ? $"\nHint: {dto.Hint}" : "");
}

