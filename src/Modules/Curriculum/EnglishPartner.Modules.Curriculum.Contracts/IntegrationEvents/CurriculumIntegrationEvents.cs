using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Contracts.IntegrationEvents;

public sealed record VocabularyCreatedIntegrationEvent(
    Guid VocabularyId,
    string Word,
    string LanguageCode) : DomainEvent;

public sealed record ExerciseCreatedIntegrationEvent(
    Guid ExerciseId,
    string Title,
    string LanguageCode) : DomainEvent;

public sealed record ConversationCreatedIntegrationEvent(
    Guid ConversationId,
    string TopicName,
    string LanguageCode) : DomainEvent;
