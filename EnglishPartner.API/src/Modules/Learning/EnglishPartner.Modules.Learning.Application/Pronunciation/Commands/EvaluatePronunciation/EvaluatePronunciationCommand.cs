using EnglishPartner.SharedKernel.CQRS;
using Microsoft.AspNetCore.Http;

namespace EnglishPartner.Modules.Learning.Application.Pronunciation.Commands.EvaluatePronunciation;

public sealed record EvaluatePronunciationCommand(
    string ExpectedText,
    IFormFile AudioFile,
    string? LanguageCode = "en-US") : ICommand<PronunciationEvaluationResponse>;

public sealed record PronunciationEvaluationResponse(
    int Score,
    string Transcription,
    string? Feedback);
