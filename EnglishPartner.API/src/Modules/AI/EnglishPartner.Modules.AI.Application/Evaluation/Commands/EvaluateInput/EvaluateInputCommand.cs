using EnglishPartner.Modules.Curriculum.Contracts.Services;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.AI.Application.Evaluation.Commands.EvaluateInput;

/// <summary>
/// Evaluates a user's answer using Gemini. Returns SM-2 compatible quality score (0-5).
///
/// Two modes:
///   1. Server-side context: provide ItemId + ItemType → AI module fetches context from Curriculum.
///   2. Client-side context: provide ItemContext directly (legacy/standalone mode).
///
/// Works for ALL content types: Vocabulary, Grammar, Exercise, ExampleSentence.
/// </summary>
public sealed record EvaluateInputCommand(
    string UserInput,
    string? ItemContext = null,
    Guid? ItemId = null,
    CurriculumItemType? ItemType = null) : ICommand<EvaluateInputResponse>;

public sealed record EvaluateInputResponse(
    int Score,
    bool IsCorrect,
    string SuggestedCorrection,
    string Explanation);

