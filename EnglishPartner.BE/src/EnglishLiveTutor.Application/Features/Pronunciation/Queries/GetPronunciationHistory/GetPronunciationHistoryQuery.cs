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

public sealed class GetPronunciationHistoryQueryHandler : IQueryHandler<GetPronunciationHistoryQuery, List<PronunciationAssessmentResultDto>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public GetPronunciationHistoryQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<PronunciationAssessmentResultDto>> HandleAsync(GetPronunciationHistoryQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.PronunciationAssessments
            .AsNoTracking()
            .Include(a => a.CriterionScores)
            .Where(a => a.UserId == query.UserId);

        if (query.VocabularyId.HasValue)
        {
            q = q.Where(a => a.VocabularyId == query.VocabularyId.Value);
        }

        var list = await q
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(query.Limit)
            .ToListAsync(ct);

        return list.Select(a => new PronunciationAssessmentResultDto(
            AssessmentId: a.Id,
            VocabularyId: a.VocabularyId,
            SenseId: a.SenseId,
            TargetType: a.TargetType,
            TargetId: a.TargetId,
            ExpectedText: a.ReferenceText,
            RecognizedText: a.RecognizedText,
            Accent: a.Accent,
            ModelCode: a.ModelCode,
            RubricCode: "ENGLISH_PRONUNCIATION_V1",
            RubricVersion: a.RubricVersion,
            OverallScore: a.OverallScore,
            Confidence: a.Confidence,
            IsAssessable: a.Status == "Completed",
            Status: a.Status,
            Criteria: a.CriterionScores.Select(c => new CriterionScoreResultDto(
                c.CriterionCode,
                c.CriterionName,
                c.CriterionDescriptionSnapshot,
                c.WeightSnapshot,
                c.RawScore,
                c.WeightedScore,
                c.Feedback
            )).ToList(),
            Strengths: DeserializeList(a.StrengthsJson),
            Improvements: DeserializeList(a.ImprovementsJson),
            SuggestedPractice: DeserializeList(a.SuggestedPracticeJson),
            CreatedAt: a.CreatedAtUtc
        )).ToList();
    }

    private static List<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
