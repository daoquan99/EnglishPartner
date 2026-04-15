using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Domain.AiRequestLogs;
using EnglishPartner.Modules.AI.Domain.ChatSessions;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using Microsoft.EntityFrameworkCore;
using EnglishPartner.SharedKernel.Application.Interceptors;
using EnglishPartner.SharedKernel.Interceptors;
using EnglishPartner.Messaging;

namespace EnglishPartner.Modules.AI.Infrastructure.Persistence;

/// <summary>
/// AI Module's isolated DbContext. Schema: "ai".
/// </summary>
public sealed class AiDbContext(
    DbContextOptions<AiDbContext> options,
    AuditableEntityInterceptor auditInterceptor,
    ConvertDomainEventsToOutboxMessagesInterceptor outboxInterceptor) : DbContext(options), IAiUnitOfWork
{
    internal DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    internal DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    internal DbSet<AiRequestLog> AiRequestLogs => Set<AiRequestLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(auditInterceptor, outboxInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ai");
        modelBuilder.Ignore<EnglishPartner.SharedKernel.Domain.DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AiDbContext).Assembly);
    }
}
