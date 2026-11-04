using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnglishLiveTutor.Application.Common.Models;

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

// --- Gemini 3.8 Live / Extended Thinking Pronunciation Audio Synthesis ---
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
