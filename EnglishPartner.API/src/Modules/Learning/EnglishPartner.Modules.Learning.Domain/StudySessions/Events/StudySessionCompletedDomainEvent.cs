using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Learning.Domain.StudySessions.Events;

public sealed record StudySessionCompletedDomainEvent(
    Guid StudySessionId,
    Guid UserId,
    int Score,
    int TotalItems) : DomainEvent;
