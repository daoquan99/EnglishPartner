namespace EnglishLiveTutor.Application.Common.Models;

// --- Gemini 3.8 Live / Extended Thinking Pronunciation Audio Synthesis ---
public sealed record SynthesizePronunciationAudioRequest(
    string ApiKey,
    string Text,
    string TargetType = "word",
    string Accent = "US",
    string VoiceName = "Aoede",
    double SpeechSpeed = 1.0,
    string ModelCode = "gemini-3.8-live"
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
    string AudioBase64
);

// --- Admin Vocabulary / Flashcard CRUD ---
public sealed record UpsertVocabularyAdminRequest(
    string Word,
    string PartOfSpeech = "noun",
    string CefrLevel = "B1",
    string DefinitionEn = "",
    string MeaningVi = "",
    string DescriptionEn = "",
    string? TopicCategory = null,
    int? FrequencyRank = null,
    string UkIpa = "",
    string UsIpa = "",
    string? StressPattern = null,
    List<string>? Synonyms = null,
    List<string>? Antonyms = null,
    List<UpsertWordFamilyItem>? WordFamilies = null,
    List<UpsertCollocationItem>? Collocations = null,
    List<UpsertExampleItem>? Examples = null,
    List<UpsertGrammarPatternItem>? GrammarPatterns = null
);

public sealed record UpsertWordFamilyItem(string WordForm, string PartOfSpeech, string? Note = null);
public sealed record UpsertCollocationItem(string Phrase, string MeaningVi, string? ExampleSentence = null);
public sealed record UpsertExampleItem(string SentenceEn, string TranslationVi, string CefrLevel = "B1");
public sealed record UpsertGrammarPatternItem(string Pattern, string ExplanationVi, string? SampleSentence = null);

// --- Admin AI Model Catalog CRUD ---
public sealed record UpsertAiModelConfigRequest(
    string DisplayName,
    string ModelCode,
    string? RoutedModelCode = null,
    bool SupportsTextGeneration = true,
    bool SupportsStructuredOutput = true,
    bool SupportsAudioInput = false,
    bool SupportsPronunciationAssessment = false,
    bool SupportsLiveAudio = false,
    bool SupportsTranscription = false,
    bool SupportsTranslation = false,
    bool SupportsStreaming = true,
    bool IsAvailable = true,
    bool IsDeprecated = false,
    bool IsEnabled = true,
    string CapabilityNote = "",
    int DisplayOrder = 10
);

// --- Admin User CRUD ---
public sealed record AdminCreateUserRequest(
    string Username,
    string Email,
    string Password,
    string FullName,
    string PreferredCefrLevel = "B1",
    string PreferredVoiceName = "Aoede",
    bool IsActive = true,
    List<string>? RoleCodes = null
);

public sealed record AdminUpdateUserRequest(
    string Email,
    string FullName,
    string PreferredCefrLevel = "B1",
    string PreferredVoiceName = "Aoede",
    bool IsActive = true,
    string? NewPassword = null,
    List<string>? RoleCodes = null
);

public sealed record AdminUpdateRoleRequest(
    string Name,
    string Description,
    bool IsActive = true,
    List<string>? PermissionCodes = null
);

public sealed record AdminUpdatePermissionRequest(
    string Name,
    string Module,
    string Description
);
