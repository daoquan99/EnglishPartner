using EnglishLiveTutor.Domain.Topics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations;

public sealed class TopicEntityConfiguration : IEntityTypeConfiguration<TopicEntity>
{
    public void Configure(EntityTypeBuilder<TopicEntity> builder)
    {
        builder.ToTable("Topics");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Code).HasMaxLength(80).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(256).IsRequired();
        builder.Property(e => e.DisplayNameVi).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(64).HasDefaultValue("General").IsRequired();
        builder.Property(e => e.ModeCode).HasMaxLength(64);
        builder.Property(e => e.SuggestedCefrLevel).HasMaxLength(8).HasDefaultValue("B1").IsRequired();
        builder.Property(e => e.Description).HasMaxLength(600);

        builder.ConfigureAudit<TopicEntity, Guid>();
        builder.ConfigureSoftDelete<TopicEntity, Guid>();

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => new { e.IsActive, e.SupportsConversation, e.SupportsVocabulary, e.DisplayOrder });
    }
}
