using EnglishLiveTutor.Domain.Prompts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations;

public sealed class CefrLevelPromptEntityConfiguration : IEntityTypeConfiguration<CefrLevelPromptEntity>
{
    public void Configure(EntityTypeBuilder<CefrLevelPromptEntity> builder)
    {
        builder.ToTable("CefrLevelPrompts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CefrLevel).HasMaxLength(8).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(128).IsRequired();
        builder.Property(e => e.LevelGuidance).HasColumnType("nvarchar(max)").IsRequired();

        builder.ConfigureAudit<CefrLevelPromptEntity, Guid>();
        builder.ConfigureSoftDelete<CefrLevelPromptEntity, Guid>();

        builder.HasIndex(e => e.CefrLevel).IsUnique();
    }
}

public sealed class ConversationModePromptEntityConfiguration : IEntityTypeConfiguration<ConversationModePromptEntity>
{
    public void Configure(EntityTypeBuilder<ConversationModePromptEntity> builder)
    {
        builder.ToTable("ConversationModePrompts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.ModeCode).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(64).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(e => e.DirectiveTemplate).HasColumnType("nvarchar(max)").IsRequired();

        builder.ConfigureAudit<ConversationModePromptEntity, Guid>();
        builder.ConfigureSoftDelete<ConversationModePromptEntity, Guid>();

        builder.HasIndex(e => e.ModeCode).IsUnique();
    }
}
