using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Messaging;

/// <summary>
/// In-process event bus that resolves handlers from DI container.
/// Suitable for modular monolith; replace with RabbitMQ/Kafka for microservices.
/// </summary>
public sealed class InMemoryEventBus(
    IServiceProvider serviceProvider,
    ILogger<InMemoryEventBus> logger) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        var eventType = @event.GetType();
        logger.LogInformation("Publishing integration event {EventType} ({EventId})",
            eventType.Name, @event.EventId);

        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = serviceProvider.GetServices(handlerType);

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
            }
        }
    }
}
