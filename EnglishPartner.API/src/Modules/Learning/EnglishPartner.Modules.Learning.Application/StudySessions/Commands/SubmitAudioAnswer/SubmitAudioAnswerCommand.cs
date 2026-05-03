using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.SharedKernel.CQRS;
using Microsoft.AspNetCore.Http;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitAudioAnswer;

public sealed record SubmitAudioAnswerCommand(
    Guid SessionId,
    Guid ItemId,
    ItemType ItemType,
    IFormFile AudioFile,
    int TimeSpentSeconds) : ICommand<SubmitAudioAnswerResponse>;

public sealed record SubmitAudioAnswerResponse(
    int QualityScore,
    string Transcription,
    string? PronunciationFeedback,
    int IntervalDays,
    double EasinessFactor,
    DateTime NextReviewDate);
