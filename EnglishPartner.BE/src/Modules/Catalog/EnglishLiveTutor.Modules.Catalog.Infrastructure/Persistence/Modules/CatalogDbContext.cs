using EnglishLiveTutor.Application.Modules.Catalog;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Domain.Voices;
using EnglishLiveTutor.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options), ICatalogDbContext
{
    public DbSet<CefrLevelPromptEntity> CefrLevelPrompts => Set<CefrLevelPromptEntity>();
    public DbSet<ConversationModePromptEntity> ConversationModePrompts => Set<ConversationModePromptEntity>();
    public DbSet<SpeechSpeedEntity> SpeechSpeeds => Set<SpeechSpeedEntity>();
    public DbSet<TeacherVoiceEntity> TeacherVoices => Set<TeacherVoiceEntity>();
    public DbSet<TopicEntity> Topics => Set<TopicEntity>();
    public DbSet<AiModelConfigEntity> AiModelCatalog => Set<AiModelConfigEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Catalog);
        modelBuilder.ApplyConfiguration(new CefrLevelPromptEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ConversationModePromptEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SpeechSpeedEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TeacherVoiceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TopicEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AiModelConfigEntityConfiguration());
    }
}
