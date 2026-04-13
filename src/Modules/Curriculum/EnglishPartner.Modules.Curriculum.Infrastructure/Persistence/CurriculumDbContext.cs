using EnglishPartner.Modules.Curriculum.Application.Abstractions;
using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Entities;
using EnglishPartner.Modules.Curriculum.Domain.Grammars;
using EnglishPartner.Modules.Curriculum.Domain.Grammars.Entities;
using EnglishPartner.Modules.Curriculum.Domain.Languages;
using EnglishPartner.Modules.Curriculum.Domain.Topics;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Entities;
using EnglishPartner.SharedKernel.Interceptors;
using EnglishPartner.SharedKernel.Application.Interceptors;
using Microsoft.EntityFrameworkCore;
using EnglishPartner.Messaging;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Curriculum module.
/// Schema: "curriculum".
/// </summary>
public sealed class CurriculumDbContext(
    DbContextOptions<CurriculumDbContext> options,
    AuditableEntityInterceptor auditInterceptor,
    ConvertDomainEventsToOutboxMessagesInterceptor outboxInterceptor) : DbContext(options), ICurriculumUnitOfWork
{
    internal const string Schema = "curriculum";

    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Vocabulary> Vocabularies => Set<Vocabulary>();
    public DbSet<VocabularySynonym> VocabularySynonyms => Set<VocabularySynonym>();
    public DbSet<Grammar> Grammars => Set<Grammar>();
    public DbSet<GrammarExample> GrammarExamples => Set<GrammarExample>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseOption> ExerciseOptions => Set<ExerciseOption>();
    public DbSet<EnglishPartner.Modules.Curriculum.Domain.Conversations.Conversation> Conversations => Set<EnglishPartner.Modules.Curriculum.Domain.Conversations.Conversation>();
    public DbSet<EnglishPartner.Modules.Curriculum.Domain.Conversations.Entities.ConversationLine> ConversationLines => Set<EnglishPartner.Modules.Curriculum.Domain.Conversations.Entities.ConversationLine>();
    public DbSet<EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.ExampleSentence> ExampleSentences => Set<EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.ExampleSentence>();
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CurriculumDbContext).Assembly);
        CurriculumDataSeeder.Seed(modelBuilder);
    }
}
