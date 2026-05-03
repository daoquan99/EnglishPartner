using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.Modules.Learning.Domain.StudySessions;
using EnglishPartner.Modules.Learning.Domain.UserProfiles;
using EnglishPartner.SharedKernel.Interceptors;
using EnglishPartner.SharedKernel.Application.Interceptors;
using Microsoft.EntityFrameworkCore;
using EnglishPartner.Modules.Learning.Application.Abstractions;
using EnglishPartner.Messaging;

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence;

public sealed class LearningDbContext(
    DbContextOptions<LearningDbContext> options,
    AuditableEntityInterceptor auditInterceptor,
    ConvertDomainEventsToOutboxMessagesInterceptor outboxInterceptor) : DbContext(options), ILearningUnitOfWork
{
    internal const string Schema = "learning";

    public DbSet<UserLearningProfile> UserLearningProfiles => Set<UserLearningProfile>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<ItemMastery> ItemMasteries => Set<ItemMastery>();
    public DbSet<LearningActivityLog> ActivityLogs => Set<LearningActivityLog>();
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LearningDbContext).Assembly);
    }
}
