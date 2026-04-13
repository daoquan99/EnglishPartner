using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Topics.Events;

public sealed record TopicCreatedDomainEvent(Guid TopicId) : DomainEvent;
