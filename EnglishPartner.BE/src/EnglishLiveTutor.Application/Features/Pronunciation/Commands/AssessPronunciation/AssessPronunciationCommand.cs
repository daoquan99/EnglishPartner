using System.Text.Json;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.AssessPronunciation;

public sealed record AssessPronunciationCommand(
    string UserId,
    SubmitPronunciationAssessmentRequest Request
) : ICommand<PronunciationAssessmentResultDto>;
