namespace EnglishLiveTutor.Application.Features.Vocabulary.Contracts;

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
