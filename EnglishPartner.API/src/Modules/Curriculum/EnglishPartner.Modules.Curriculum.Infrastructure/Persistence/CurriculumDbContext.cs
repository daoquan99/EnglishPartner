using EnglishPartner.Modules.Curriculum.Application.Abstractions;
using EnglishPartner.Modules.Curriculum.Domain.Conversations;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Entities;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Entities;
using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Entities;
using EnglishPartner.Modules.Curriculum.Domain.Grammars;
using EnglishPartner.Modules.Curriculum.Domain.Grammars.Entities;
using EnglishPartner.Modules.Curriculum.Domain.Languages;
using EnglishPartner.Modules.Curriculum.Domain.Topics;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies;
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

    public DbSet<Grammar> Grammars => Set<Grammar>();
    public DbSet<GrammarExample> GrammarExamples => Set<GrammarExample>();
    public DbSet<GrammarExampleTranslation> GrammarExampleTranslations => Set<GrammarExampleTranslation>();

    public DbSet<ExampleSentence> ExampleSentences => Set<ExampleSentence>();
    public DbSet<ExampleSentenceTranslation> ExampleSentenceTranslations => Set<ExampleSentenceTranslation>();

    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseOption> ExerciseOptions => Set<ExerciseOption>();

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationLine> ConversationLines => Set<ConversationLine>();
    public DbSet<ConversationLineTranslation> ConversationLineTranslations => Set<ConversationLineTranslation>();

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
