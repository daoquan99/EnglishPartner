using EnglishPartner.SharedKernel.Domain;
using MediatR;

namespace EnglishPartner.SharedKernel.Events;

/// <summary>
/// Wraps a domain event as a MediatR INotification.
/// Used by the outbox or event dispatcher to publish domain events via MediatR pipeline.
/// </summary>
public sealed record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : IDomainEvent;
