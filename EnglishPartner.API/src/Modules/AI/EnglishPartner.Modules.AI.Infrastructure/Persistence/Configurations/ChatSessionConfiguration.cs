using EnglishPartner.Modules.AI.Domain.ChatSessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.AI.Infrastructure.Persistence.Configurations;

internal sealed class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("chat_sessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SystemPrompt).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Topic).HasMaxLength(256).IsRequired(false);
        builder.Property(x => x.SessionType).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.VoiceName).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.VoiceLanguageCode).HasMaxLength(16).IsRequired(false);
        builder.Property(x => x.VoiceGender).HasMaxLength(16).IsRequired(false);

        builder.HasMany(x => x.Messages)
            .WithOne()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Summaries)
            .WithOne()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.UserId);
    }
}

internal sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SessionId).IsRequired();
        builder.Property(x => x.Role).IsRequired().HasConversion<string>();
        builder.Property(x => x.Text).IsRequired();
        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.AudioUrl).HasMaxLength(1000).IsRequired(false);
        builder.Property(x => x.Translation).HasMaxLength(4000).IsRequired(false);
        // Timestamp removed: CreatedAt from Entity base class serves this purpose

        builder.HasIndex(x => x.SessionId);
    }
}

internal sealed class ChatHistorySummaryConfiguration : IEntityTypeConfiguration<ChatHistorySummary>
{
    public void Configure(EntityTypeBuilder<ChatHistorySummary> builder)
    {
        builder.ToTable("chat_history_summaries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SessionId).IsRequired();
        builder.Property(x => x.StartOrder).IsRequired();
        builder.Property(x => x.EndOrder).IsRequired();
        builder.Property(x => x.Summary).IsRequired();

        builder.HasIndex(x => x.SessionId);
    }
}
