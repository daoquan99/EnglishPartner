namespace EnglishLiveTutor.Application.Common.Models;

public sealed record SubmitPronunciationAssessmentRequest(
    string ApiKey,
    Guid VocabularyId,
    Guid SenseId,
    string TargetType = "Word", // Word | Sentence
    Guid? TargetId = null,
    string? ReferenceText = null,
    string Accent = "US", // US | UK
    string AssessmentModelCode = "gemini-3.8-flash",
    string Base64Audio = "",
    string ContentType = "audio/webm",
    int DurationMilliseconds = 2500
);

public sealed record CriterionScoreResultDto(
    string Code,
    string Name,
    string Description,
    decimal Weight,
    decimal Score,
    decimal WeightedScore,
    string Feedback
);

public sealed record PronunciationAssessmentResultDto(
    Guid AssessmentId,
    Guid VocabularyId,
    Guid SenseId,
    string TargetType,
    Guid? TargetId,
    string ExpectedText,
    string? RecognizedText,
    string Accent,
    string ModelCode,
    string RubricCode,
    int RubricVersion,
    decimal? OverallScore,
    decimal? Confidence,
    bool IsAssessable,
    string Status,
    List<CriterionScoreResultDto> Criteria,
    List<string> Strengths,
    List<string> Improvements,
    List<string> SuggestedPractice,
    DateTimeOffset CreatedAt
);

public sealed record UpdateRubricCriterionRequest(
    string Code,
    string Name,
    string Description,
    decimal Weight
);

public sealed record UpdateScoringRubricRequest(
    string Name,
    string Description,
    List<UpdateRubricCriterionRequest> Criteria
);
