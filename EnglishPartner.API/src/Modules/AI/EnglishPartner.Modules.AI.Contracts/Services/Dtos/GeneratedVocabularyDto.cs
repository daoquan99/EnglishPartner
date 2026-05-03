namespace EnglishPartner.Modules.AI.Contracts.Services.Dtos;

/// <summary>AI-generated vocabulary content ready to be persisted.</summary>
public sealed record GeneratedVocabularyDto(
    string Word,
    string Definition,
    string? Pronunciation,
    string? PhoneticTranscription,
    string? PartOfSpeech,
    string? Level,
    List<string> Synonyms);
