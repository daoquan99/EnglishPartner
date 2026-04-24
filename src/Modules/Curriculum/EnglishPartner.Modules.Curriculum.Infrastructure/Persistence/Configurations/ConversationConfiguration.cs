using EnglishPartner.Modules.Curriculum.Domain.Conversations;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Scenario).HasMaxLength(1000);
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Level).HasMaxLength(20);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);
        builder.Property(x => x.IsAiGenerated).HasDefaultValue(false);

        builder.HasIndex(x => x.LanguageCode);
        builder.HasIndex(x => x.TopicId);
        builder.HasIndex(x => x.GrammarId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ConversationLineConfiguration : IEntityTypeConfiguration<ConversationLine>
{
    public void Configure(EntityTypeBuilder<ConversationLine> builder)
    {
        builder.ToTable("conversation_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Speaker).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Text).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.AudioUrl).HasMaxLength(500);

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(x => x.ConversationLineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ConversationLineTranslationConfiguration : IEntityTypeConfiguration<ConversationLineTranslation>
{
    public void Configure(EntityTypeBuilder<ConversationLineTranslation> builder)
    {
        builder.ToTable("conversation_line_translations");
        builder.HasKey(x => x.Id); 
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.TranslatedText).HasMaxLength(2000).IsRequired();

        // One translation per language per line
        builder.HasIndex(x => new { x.ConversationLineId, x.LanguageCode }).IsUnique();
    }
}
