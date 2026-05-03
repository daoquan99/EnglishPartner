using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Events;

public sealed record VocabularyCreatedDomainEvent(Guid VocabularyId) : DomainEvent;
