using EnglishLiveTutor.Application.Modules.LiveTutor;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Entities;
using EnglishLiveTutor.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules;

public sealed class LiveTutorDbContext(DbContextOptions<LiveTutorDbContext> options)
    : DbContext(options), ILiveTutorDbContext
{
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<ChatMessageEntity> Messages => Set<ChatMessageEntity>();
    public DbSet<LanguageCorrectionEntity> Corrections => Set<LanguageCorrectionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.LiveTutor);
        modelBuilder.ApplyConfiguration(new ConversationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ChatMessageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LanguageCorrectionEntityConfiguration());
    }
}
