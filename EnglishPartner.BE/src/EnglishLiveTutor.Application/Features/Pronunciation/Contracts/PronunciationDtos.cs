using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

public sealed class LenientNullableGuidConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? raw = reader.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (Guid.TryParse(raw, out var parsed))
            {
                return parsed;
            }

            // Fallback for non-guid string tokens (e.g. "ex1", mock/draft IDs) without failing request parsing
            return null;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public sealed record SynthesizePronunciationAudioRequest(
    string? ApiKey = null,
    string? Text = null,
    string TargetType = "word",
    string Accent = "US",
    string VoiceName = "Aoede",
    double SpeechSpeed = 1.0,
    string ModelCode = "gemini-3.8-live",
    [property: JsonConverter(typeof(LenientNullableGuidConverter))] Guid? TargetId = null,
    [property: JsonConverter(typeof(LenientNullableGuidConverter))] Guid? VocabularyId = null,
    [property: JsonConverter(typeof(LenientNullableGuidConverter))] Guid? SenseId = null
);

public sealed record SynthesizePronunciationAudioResponseDto(
    string Text,
    string TargetType,
    string Accent,
    string VoiceName,
    double SpeechSpeed,
    string ModelCodeUsed,
    string SystemPromptUsed,
    string MimeType,
    int SampleRate,
    int DurationMs,
    string AudioBase64,
    bool IsComplete = true
);

public sealed record SubmitPronunciationAssessmentRequest(
    string ApiKey,
    Guid VocabularyId,
    Guid SenseId,
    string TargetType = "Word",
    Guid? TargetId = null,
    string? ReferenceText = null,
    string Accent = "US",
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

public sealed record ScoringRubricCriterionDto(
    Guid Id,
    Guid RubricId,
    string Code,
    string Name,
    string Description,
    decimal Weight,
    decimal MinimumScore,
    decimal MaximumScore,
    int DisplayOrder,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    string? UpdatedBy,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record ScoringRubricDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int Version,
    string AssessmentType,
    bool IsActive,
    List<ScoringRubricCriterionDto> Criteria,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    string? UpdatedBy,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record PronunciationAiRawResult(
    string? RecognizedText,
    decimal? Confidence,
    bool IsAssessable,
    Dictionary<string, (decimal RawScore, string Feedback)> CriterionScores,
    List<string> Strengths,
    List<string> Improvements,
    List<string> SuggestedPractice
);
