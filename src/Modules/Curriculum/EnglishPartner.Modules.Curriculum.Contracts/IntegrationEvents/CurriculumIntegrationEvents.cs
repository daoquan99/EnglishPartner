using EnglishPartner.Messaging;

namespace EnglishPartner.Modules.Curriculum.Contracts.IntegrationEvents;

public sealed record VocabularyCreatedIntegrationEvent(
    Guid VocabularyId,
    string Word,
    string LanguageCode) : IntegrationEvent;

public sealed record ExerciseCreatedIntegrationEvent(
    Guid ExerciseId,
    string Title,
    string LanguageCode) : IntegrationEvent;

public sealed record ConversationCreatedIntegrationEvent(
    Guid ConversationId,
    string TopicName,
    string LanguageCode) : IntegrationEvent;
