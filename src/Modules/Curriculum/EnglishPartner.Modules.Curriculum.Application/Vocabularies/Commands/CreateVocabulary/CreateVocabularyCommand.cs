using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Commands.CreateVocabulary;

public sealed record CreateVocabularyCommand(
    string Word,
    string Definition,
    string LanguageCode,
    string? Pronunciation,
    string? PhoneticTranscription,
    string? PartOfSpeech,
    string? ExampleSentence,
    string? ExampleTranslation,
    string? Level,
    Guid? TopicId) : ICommand<Guid>;
