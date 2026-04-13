using EnglishPartner.Messaging;
using EnglishPartner.SharedKernel.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using EnglishPartner.SharedKernel.Events;
using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.SharedKernel.Application.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob<TDbContext>(
    TDbContext dbContext,
    IPublisher publisher,
    IOptions<OutboxSettings> outboxOptions,
    ILogger<ProcessOutboxMessagesJob<TDbContext>> logger) : IJob
    where TDbContext : DbContext
{
    public async Task Execute(IJobExecutionContext context)
    {
        var batchSize = outboxOptions.Value.BatchSize;

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOn == null && m.Error == null)
            .OrderBy(m => m.OccurredOn)
            .Take(batchSize)
            .ToListAsync(context.CancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                var domainEvent = JsonConvert.DeserializeObject(
                    message.Payload,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });

                if (domainEvent is null)
                {
                    logger.LogWarning("Outbox payload deserialized to null. ID: {Id}", message.Id);
                    message.Error = "Deserialized to null.";
                    continue;
                }

                // Publish through MediatR so handlers (like Domain -> Integration mappers) can pick it up
                
                if (domainEvent is IDomainEvent validDomainEvent)
                {
                    // Use reflection/dynamic to instantiate the generic DomainEventNotification<T> type
                    var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                    var notification = Activator.CreateInstance(notificationType, domainEvent);
                    if (notification != null)
                    {
                        await publisher.Publish(notification, context.CancellationToken);
                    }
                }
                else
                {
                    await publisher.Publish(domainEvent, context.CancellationToken);
                }

                message.ProcessedOn = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
                message.Error = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
