using System.Text;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

public sealed record PronunciationAudioSynthesisRequest(
    string? ApiKey,
    string Text,
    string TargetType,
    Guid TargetId,
    string? VoiceName,
    double SpeechSpeed,
    string? Accent,
    string? ModelCode,
    string MissingApiKeyMessage,
    string IncompleteAudioMessage,
    Guid? ProviderTargetId = null,
    Guid? VocabularyId = null,
    Guid? SenseId = null,
    string? ProviderTargetType = null,
    bool ReturnStoredAudioBytesOnSynthesis = false)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append(nameof(ApiKey)).Append(" = ")
            .Append(ApiKey is null ? "null" : "***")
            .Append(", ").Append(nameof(Text)).Append(" = ").Append(Text)
            .Append(", ").Append(nameof(TargetType)).Append(" = ").Append(TargetType)
            .Append(", ").Append(nameof(TargetId)).Append(" = ").Append(TargetId)
            .Append(", ").Append(nameof(VoiceName)).Append(" = ").Append(VoiceName)
            .Append(", ").Append(nameof(SpeechSpeed)).Append(" = ").Append(SpeechSpeed)
            .Append(", ").Append(nameof(Accent)).Append(" = ").Append(Accent)
            .Append(", ").Append(nameof(ModelCode)).Append(" = ").Append(ModelCode)
            .Append(", ").Append(nameof(MissingApiKeyMessage)).Append(" = ").Append(MissingApiKeyMessage)
            .Append(", ").Append(nameof(IncompleteAudioMessage)).Append(" = ").Append(IncompleteAudioMessage)
            .Append(", ").Append(nameof(ProviderTargetId)).Append(" = ").Append(ProviderTargetId)
            .Append(", ").Append(nameof(VocabularyId)).Append(" = ").Append(VocabularyId)
            .Append(", ").Append(nameof(SenseId)).Append(" = ").Append(SenseId)
            .Append(", ").Append(nameof(ProviderTargetType)).Append(" = ").Append(ProviderTargetType)
            .Append(", ").Append(nameof(ReturnStoredAudioBytesOnSynthesis)).Append(" = ")
            .Append(ReturnStoredAudioBytesOnSynthesis);
        return true;
    }
}

public sealed record PronunciationAudioSynthesisResult(
    string AudioBase64,
    string MimeType,
    int SampleRate,
    int DurationMs);

public interface IPronunciationAudioSynthesizer
{
    Task<PronunciationAudioSynthesisResult> SynthesizeAsync(
        PronunciationAudioSynthesisRequest request,
        CancellationToken ct = default);
}
