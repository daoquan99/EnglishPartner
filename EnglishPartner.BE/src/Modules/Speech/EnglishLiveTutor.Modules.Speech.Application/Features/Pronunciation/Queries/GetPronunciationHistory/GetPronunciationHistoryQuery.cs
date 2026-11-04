using System.Text.Json;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationHistory;

public sealed record GetPronunciationHistoryQuery(
    string UserId,
    Guid? VocabularyId,
    int Limit = 20
) : IQuery<List<PronunciationAssessmentResultDto>>;
