using EnglishPartner.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF configuration for the Transactional Outbox pattern.
/// Critical: ProcessedOn + OccurredOn must be indexed — polled every 10s by Quartz.
/// </summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Payload).IsRequired();

        // Performance-critical indexes: polled every OutboxSettings.PollingIntervalSeconds
        builder.HasIndex(x => x.ProcessedOn);
        builder.HasIndex(x => x.OccurredOn);
        builder.HasIndex(x => new { x.ProcessedOn, x.RetryCount });
    }
}

/// <summary>
/// EF configuration for the Inbox idempotency pattern.
/// </summary>
internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();

        // AnyAsync(m => m.Id == eventId) uses PK — already indexed via HasKey
        builder.HasIndex(x => x.OccurredOn);
    }
}
