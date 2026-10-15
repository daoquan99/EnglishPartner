using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations;

public sealed class ConversationEntityConfiguration : IEntityTypeConfiguration<ConversationEntity>
{
    public void Configure(EntityTypeBuilder<ConversationEntity> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Id).HasMaxLength(64);
        builder.Property(e => e.Title).HasMaxLength(256).IsRequired();
        builder.Property(e => e.CefrLevel).HasMaxLength(16).IsRequired();
        builder.Property(e => e.VoiceName).HasMaxLength(64).IsRequired();
        builder.Property(e => e.ConversationMode).HasMaxLength(64).IsRequired().HasDefaultValue("daily_conversation");
        builder.Property(e => e.Topic).HasMaxLength(512).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(e => e.CustomSystemPrompt).HasColumnType("nvarchar(max)");

        builder.ConfigureAudit<ConversationEntity, string>(
            createdAtColumnName: "CreatedAt",
            updatedAtColumnName: "UpdatedAt");
        builder.ConfigureSoftDelete<ConversationEntity, string>();

        builder.HasMany(e => e.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Messages)
            .HasField("_messages")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Corrections)
            .WithOne()
            .HasForeignKey(c => c.ConversationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Corrections)
            .HasField("_corrections")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ChatMessageEntityConfiguration : IEntityTypeConfiguration<ChatMessageEntity>
{
    public void Configure(EntityTypeBuilder<ChatMessageEntity> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(e => e.ConversationId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.TurnId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Speaker).HasMaxLength(16).IsRequired();
        builder.Property(e => e.TranscriptLine1).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.TranslationLine2).HasColumnType("nvarchar(max)").IsRequired();

        builder.ConfigureAudit<ChatMessageEntity, string>(
            createdAtColumnName: "CreatedAt",
            updatedAtColumnName: "UpdatedAtUtc");

        builder.HasIndex(e => new { e.ConversationId, e.TurnId, e.Speaker }).IsUnique();
        builder.HasIndex(e => new { e.ConversationId, e.SequenceOrder });
    }
}

public sealed class LanguageCorrectionEntityConfiguration : IEntityTypeConfiguration<LanguageCorrectionEntity>
{
    public void Configure(EntityTypeBuilder<LanguageCorrectionEntity> builder)
    {
        builder.ToTable("Corrections");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(64).ValueGeneratedNever();
        builder.Property(e => e.ConversationId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.TurnId).HasMaxLength(64);
        builder.Property(e => e.OriginalText).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.CorrectedText).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.ErrorCategory).HasMaxLength(64).IsRequired();
        builder.Property(e => e.ExplanationVi).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.CefrTip).HasColumnType("nvarchar(max)").IsRequired();

        builder.ConfigureAudit<LanguageCorrectionEntity, string>(
            createdAtColumnName: "CreatedAt",
            updatedAtColumnName: "UpdatedAtUtc");

        builder.HasIndex(e => e.ConversationId);
    }
}
