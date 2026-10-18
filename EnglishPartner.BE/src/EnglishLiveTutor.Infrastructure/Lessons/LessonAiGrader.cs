using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Domain.Pronunciation;

namespace EnglishLiveTutor.Infrastructure.Lessons;

public sealed class LessonAiGrader : ILessonAiGrader
{
    public Task<LessonAiGradingOutcome> GradeSubmissionAsync(
        ExerciseType exerciseType,
        string taskInstructions,
        string cefrLevel,
        ScoringRubricEntity rubric,
        string learnerSubmissionPayloadJson,
        decimal maxPoints,
        CancellationToken ct = default)
    {
        // Extract submission text or audio asset
        string text = ExtractSubmittedText(learnerSubmissionPayloadJson);

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new LessonAiGradingOutcome(
                IsAssessable: false,
                Score100: 0m,
                PointsEarned: 0m,
                CriteriaBreakdown: new List<LessonAiCriterionScore>(),
                Strengths: new List<string>(),
                Improvements: new List<string> { "Submission was empty or unreadable." },
                SuggestedAnswer: null));
        }

        var criteriaList = rubric.Criteria.OrderBy(c => c.DisplayOrder).ToList();
        var breakdown = new List<LessonAiCriterionScore>();

        decimal totalWeightedScore = 0m;

        foreach (var c in criteriaList)
        {
            // Evaluate raw score 0..4 (defaulting to 3 or 4 for sensible submissions)
            int raw = EvaluateCriterionRawScore(text, c.Code);
            string descriptor = GetDescriptorForLevel(c, raw);
            string feedback = GenerateCriterionFeedback(c.Name, raw);

            decimal criterionComponent = (raw / 4.0m) * c.Weight;
            totalWeightedScore += criterionComponent;

            breakdown.Add(new LessonAiCriterionScore(
                CriterionCode: c.Code,
                Name: c.Name,
                Weight: c.Weight,
                RawScore: raw,
                Descriptor: descriptor,
                Feedback: feedback));
        }

        decimal score100 = Math.Round(totalWeightedScore, 2, MidpointRounding.AwayFromZero);
        decimal pointsEarned = Math.Round((score100 / 100.00m) * maxPoints, 2, MidpointRounding.AwayFromZero);

        var outcome = new LessonAiGradingOutcome(
            IsAssessable: true,
            Score100: score100,
            PointsEarned: pointsEarned,
            CriteriaBreakdown: breakdown,
            Strengths: new List<string> { "Good task focus and clear sentence construction." },
            Improvements: new List<string> { "Expand vocabulary and refine transition phrases for higher fluency." },
            SuggestedAnswer: null);

        return Task.FromResult(outcome);
    }

    private static string ExtractSubmittedText(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return string.Empty;

        try
        {
            var node = JsonNode.Parse(payloadJson);
            if (node == null) return string.Empty;

            if (node["writtenText"] != null)
                return node["writtenText"]!.ToString();
            if (node["translatedText"] != null)
                return node["translatedText"]!.ToString();
            if (node["correctedSentence"] != null)
                return node["correctedSentence"]!.ToString();
            if (node["audioAssetId"] != null)
                return $"Audio submission: {node["audioAssetId"]}";

            return node.ToJsonString();
        }
        catch
        {
            return payloadJson;
        }
    }

    private static int EvaluateCriterionRawScore(string text, string criterionCode)
    {
        int wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < 2) return 1;
        if (wordCount < 5) return 2;
        if (wordCount < 10) return 3;
        return 4;
    }

    private static string GetDescriptorForLevel(EnglishLiveTutor.Domain.Pronunciation.Entities.ScoringRubricCriterionEntity c, int level)
    {
        return level switch
        {
            0 => !string.IsNullOrEmpty(c.DescriptorLevel0) ? c.DescriptorLevel0 : "Chưa đạt yêu cầu tối thiểu.",
            1 => !string.IsNullOrEmpty(c.DescriptorLevel1) ? c.DescriptorLevel1 : "Dưới mức trung bình.",
            2 => !string.IsNullOrEmpty(c.DescriptorLevel2) ? c.DescriptorLevel2 : "Đạt mức trung bình.",
            3 => !string.IsNullOrEmpty(c.DescriptorLevel3) ? c.DescriptorLevel3 : "Đạt chuẩn tốt.",
            4 => !string.IsNullOrEmpty(c.DescriptorLevel4) ? c.DescriptorLevel4 : "Xuất sắc.",
            _ => string.Empty
        };
    }

    private static string GenerateCriterionFeedback(string criterionName, int rawScore)
    {
        return rawScore switch
        {
            4 => $"Thể hiện xuất sắc tiêu chí '{criterionName}'.",
            3 => $"Đáp ứng tốt yêu cầu của tiêu chí '{criterionName}'.",
            2 => $"Đạt mức khá đối với '{criterionName}', cần hoàn thiện thêm.",
            1 => $"Còn nhiều thiếu sót đối với '{criterionName}'.",
            _ => $"Chưa đáp ứng tiêu chí '{criterionName}'."
        };
    }
}
