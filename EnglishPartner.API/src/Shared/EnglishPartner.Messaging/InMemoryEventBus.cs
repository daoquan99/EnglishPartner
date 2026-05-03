using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Messaging;

/// <summary>
/// In-process event bus that resolves handlers from DI container.
/// Suitable for modular monolith; replace with RabbitMQ/Kafka for microservices.
/// Exceptions from handlers are NOT swallowed — they propagate to the outbox job
/// so failed messages get an Error stamp and are retried on the next poll.
/// </summary>
public sealed class InMemoryEventBus(
    IServiceScopeFactory scopeFactory,
    ILogger<InMemoryEventBus> logger) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        var eventType = @event.GetType();
        logger.LogInformation("Publishing integration event {EventType} ({EventId})",
            eventType.Name, @event.EventId);

        using var scope = scopeFactory.CreateScope();
        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        logger.LogInformation("Found {HandlerCount} handlers for {EventType}",
            handlers.Count(), eventType.Name);

        var errors = new List<Exception>();

        foreach (var handler in handlers)
        {
            try
            {
                var method = handlerType.GetMethod(nameof(IIntegrationEventHandler<IntegrationEvent>.HandleAsync));
                if (method is not null)
                {
                    var task = (Task)method.Invoke(handler, [@event, cancellationToken])!;
                    await task;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling integration event {EventType} in {HandlerType}",
                    eventType.Name, handler?.GetType().Name);
                errors.Add(ex);
            }
        }

        // Rethrow so the outbox job marks this message with Error and schedules a retry.
        if (errors.Count == 1) throw errors[0];
        if (errors.Count > 1) throw new AggregateException($"Multiple handlers failed for {eventType.Name}", errors);
    }
}
