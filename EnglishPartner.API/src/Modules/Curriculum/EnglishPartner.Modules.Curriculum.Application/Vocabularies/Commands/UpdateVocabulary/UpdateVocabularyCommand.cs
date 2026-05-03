using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Commands.UpdateVocabulary;

public sealed record UpdateVocabularyCommand(
    Guid Id,
    string Word,
    string Definition,
    string? Pronunciation,
    string? PhoneticTranscription,
    string? PartOfSpeech,
    string? ImageUrl,
    string? AudioUrl,
    string? Level,
    Guid? TopicId,
    List<string>? Synonyms) : ICommand;
