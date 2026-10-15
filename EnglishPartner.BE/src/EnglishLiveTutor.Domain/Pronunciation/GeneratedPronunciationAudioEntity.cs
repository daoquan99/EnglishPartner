using System.Security.Cryptography;
using System.Text;
using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Pronunciation;

public sealed class GeneratedPronunciationAudioEntity : AggregateRoot<Guid>
{
    public string TargetType { get; private set; } = "word"; // word | sentence
    public Guid TargetId { get; private set; }
    public string VoiceName { get; private set; } = "Aoede";
    public decimal SpeechSpeed { get; private set; } = 1.00m;
    public string Accent { get; private set; } = "US"; // US | UK
    public string ModelCode { get; private set; } = "gemini-3.8-live";
    public string TextSnapshot { get; private set; } = string.Empty;
    public string TextHash { get; private set; } = string.Empty;
    public byte[] AudioData { get; private set; } = [];
    public string MimeType { get; private set; } = "audio/wav";
    public int SampleRate { get; private set; } = 24000;
    public int DurationMs { get; private set; }
    public string SystemPromptUsed { get; private set; } = string.Empty;
    public bool IsValid { get; private set; } = true;

    private GeneratedPronunciationAudioEntity()
    {
    }

    public GeneratedPronunciationAudioEntity(
        string targetType,
        Guid targetId,
        string voiceName,
        decimal speechSpeed,
        string accent,
        string modelCode,
        string textSnapshot,
        string textHash,
        byte[] audioData,
        string mimeType = "audio/wav",
        int sampleRate = 24000,
        int durationMs = 0,
        string? systemPromptUsed = null,
        bool isValid = true,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (targetId == Guid.Empty)
            throw new InvalidOperationException("TargetId không được để trống.");
        if (string.IsNullOrWhiteSpace(targetType))
            throw new InvalidOperationException("TargetType không được để trống.");
        if (string.IsNullOrWhiteSpace(voiceName))
            throw new InvalidOperationException("VoiceName không được để trống.");
        if (string.IsNullOrWhiteSpace(textSnapshot))
            throw new InvalidOperationException("TextSnapshot không được để trống.");
        if (audioData is null || audioData.Length == 0)
            throw new InvalidOperationException("Dữ liệu âm thanh audioData không được để trống.");

        TargetType = targetType.Trim().ToLowerInvariant();
        TargetId = targetId;
        VoiceName = voiceName.Trim();
        SpeechSpeed = Math.Round(speechSpeed, 2, MidpointRounding.AwayFromZero);
        Accent = string.IsNullOrWhiteSpace(accent) ? "US" : accent.Trim().ToUpperInvariant();
        ModelCode = string.IsNullOrWhiteSpace(modelCode) ? "gemini-3.8-live" : modelCode.Trim();
        TextSnapshot = textSnapshot.Trim();
        TextHash = string.IsNullOrWhiteSpace(textHash) ? ComputeTextHash(TextSnapshot) : textHash.Trim().ToLowerInvariant();
        AudioData = audioData;
        MimeType = string.IsNullOrWhiteSpace(mimeType) ? "audio/wav" : mimeType.Trim();
        SampleRate = sampleRate <= 0 ? 24000 : sampleRate;
        DurationMs = durationMs;
        SystemPromptUsed = systemPromptUsed?.Trim() ?? string.Empty;
        IsValid = isValid;
    }

    public static string ComputeTextHash(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public void MarkInvalid()
    {
        IsValid = false;
    }
}
