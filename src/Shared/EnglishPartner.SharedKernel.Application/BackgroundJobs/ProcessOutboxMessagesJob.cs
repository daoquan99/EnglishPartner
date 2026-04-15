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
        var settings  = outboxOptions.Value;
        var maxRetries = settings.MaxRetries;

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOn == null && m.RetryCount < maxRetries)
            .OrderBy(m => m.OccurredOn)
            .Take(settings.BatchSize)
            .ToListAsync(context.CancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            // Clear previous transient error before retrying
            message.Error = null;

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
                    message.RetryCount = maxRetries; // Permanent failure — skip on next polls
                    continue;
                }

                // Publish through MediatR so handlers (like Domain -> Integration mappers) can pick it up
                if (domainEvent is IDomainEvent validDomainEvent)
                {
                    var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                    var notification     = Activator.CreateInstance(notificationType, domainEvent);
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
                message.RetryCount++;
                message.Error = ex.Message;
                logger.LogError(ex,
                    "Failed to process outbox message {Id} (attempt {Attempt}/{Max})",
                    message.Id, message.RetryCount, maxRetries);

                if (message.RetryCount >= maxRetries)
                {
                    logger.LogCritical(
                        "Outbox message {Id} (type={Type}) exhausted all {Max} retries. Manual intervention required.",
                        message.Id, message.Type, maxRetries);
                }
            }
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
