using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.SharedKernel.Application.Interceptors;
using EnglishPartner.SharedKernel.Interceptors;
using EnglishPartner.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationDbContext(
    DbContextOptions<NotificationDbContext> options,
    AuditableEntityInterceptor auditInterceptor,
    ConvertDomainEventsToOutboxMessagesInterceptor outboxInterceptor) : DbContext(options), INotificationUnitOfWork
{
    internal const string Schema = "notifications";

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(auditInterceptor, outboxInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Ignore<EnglishPartner.SharedKernel.Domain.DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
    }
}
