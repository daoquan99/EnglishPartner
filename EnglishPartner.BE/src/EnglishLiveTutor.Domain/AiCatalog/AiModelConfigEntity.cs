using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.AiCatalog;

public sealed class AiModelConfigEntity : AggregateRoot<Guid>
{
    public string DisplayName { get; private set; } = string.Empty;
    public string ModelCode { get; private set; } = string.Empty;
    public string? RoutedModelCode { get; private set; }
    public bool SupportsTextGeneration { get; private set; }
    public bool SupportsStructuredOutput { get; private set; }
    public bool SupportsAudioInput { get; private set; }
    public bool SupportsPronunciationAssessment { get; private set; }
    public bool SupportsLiveAudio { get; private set; }
    public bool SupportsTranscription { get; private set; }
    public bool SupportsTranslation { get; private set; }
    public bool SupportsTts { get; private set; }
    public bool SupportsStreaming { get; private set; }
    public bool IsAvailable { get; private set; } = true;
    public bool IsDeprecated { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public string? CapabilityNote { get; private set; }
    public int DisplayOrder { get; private set; }

    private AiModelConfigEntity()
    {
    }

    public AiModelConfigEntity(
        string displayName,
        string modelCode,
        string? routedModelCode = null,
        bool supportsTextGeneration = false,
        bool supportsStructuredOutput = false,
        bool supportsAudioInput = false,
        bool supportsPronunciationAssessment = false,
        bool supportsLiveAudio = false,
        bool supportsTranscription = false,
        bool supportsTranslation = false,
        bool supportsTts = false,
        bool supportsStreaming = false,
        bool isAvailable = true,
        bool isDeprecated = false,
        bool isEnabled = true,
        string? capabilityNote = null,
        int displayOrder = 0,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(modelCode))
            throw new ArgumentException("Model code is required.", nameof(modelCode));

        DisplayName = displayName.Trim();
        ModelCode = modelCode.Trim();
        RoutedModelCode = string.IsNullOrWhiteSpace(routedModelCode) ? null : routedModelCode.Trim();
        SupportsTextGeneration = supportsTextGeneration;
        SupportsStructuredOutput = supportsStructuredOutput;
        SupportsAudioInput = supportsAudioInput;
        SupportsPronunciationAssessment = supportsPronunciationAssessment;
        SupportsLiveAudio = supportsLiveAudio;
        SupportsTranscription = supportsTranscription;
        SupportsTranslation = supportsTranslation;
        SupportsTts = supportsTts;
        SupportsStreaming = supportsStreaming;
        IsAvailable = isAvailable;
        IsDeprecated = isDeprecated;
        IsEnabled = isEnabled;
        CapabilityNote = capabilityNote;
        DisplayOrder = displayOrder;
    }

    public string GetEffectiveModelCode()
    {
        return IsDeprecated && !string.IsNullOrWhiteSpace(RoutedModelCode)
            ? RoutedModelCode!
            : ModelCode;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
