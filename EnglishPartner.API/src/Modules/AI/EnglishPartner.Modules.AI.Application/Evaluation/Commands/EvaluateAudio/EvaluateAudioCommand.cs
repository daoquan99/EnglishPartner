using EnglishPartner.Modules.Curriculum.Contracts.Services;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.AI.Application.Evaluation.Commands.EvaluateAudio;

public sealed record EvaluateAudioCommand(
    Stream AudioStream,
    string FileName,
    string ContentType,
    string? ItemContext = null,
    Guid? ItemId = null,
    CurriculumItemType? ItemType = null) : ICommand<EvaluateAudioResponse>;

public sealed record EvaluateAudioResponse(
    string AudioUrl,
    string Transcription,
    int Score,
    bool IsCorrect,
    string SuggestedCorrection,
    string Explanation);

