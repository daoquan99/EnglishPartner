using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Messaging;

/// <summary>
/// Base class for Integration Event Handlers to ensure Idempotency via the Inbox pattern.
/// Ensures that an event is processed exactly once per module's DbContext.
/// </summary>
public abstract class IdempotentIntegrationEventHandler<TEvent, TDbContext>(
    TDbContext dbContext,
    TimeProvider timeProvider,
    ILogger logger) : IIntegrationEventHandler<TEvent>
    where TEvent : IntegrationEvent
    where TDbContext : DbContext
{
    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
    {
        // 1. Check if the event has already been processed in this module's inbox
        var isProcessed = await dbContext.Set<InboxMessage>()
            .AnyAsync(m => m.Id == @event.EventId, cancellationToken);

        if (isProcessed)
        {
            logger.LogInformation("Integration event {EventId} of type {EventType} was already processed. Skipping.", 
                @event.EventId, @event.GetType().Name);
            return;
        }

        // 2. Execute the actual business logic provided by the derived class
        try
        {
            await HandleInternalAsync(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process integration event {EventId} of type {EventType}.", 
                @event.EventId, @event.GetType().Name);
            throw; // Re-throw to allow retry mechanism (if bus supports it) or mark as failed.
        }

        // 3. Mark as processed in the Inbox
        var inboxMessage = new InboxMessage
        {
            Id = @event.EventId,
            Name = @event.GetType().Name,
            OccurredOn = @event.OccurredAt,
            ProcessedOn = timeProvider.GetUtcNow().UtcDateTime
        };

        dbContext.Set<InboxMessage>().Add(inboxMessage);

        // Save changes within the handler to commit both the business data changes and the inbox state atomically.
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Override this method to implement the specific logic for handling the integration event.
    /// Do NOT call SaveChangesAsync here; the base class will handle it automatically along with the Inbox state.
    /// </summary>
    protected abstract Task HandleInternalAsync(TEvent @event, CancellationToken cancellationToken);
}
