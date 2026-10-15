using EnglishLiveTutor.Domain.Voices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations;

public sealed class TeacherVoiceEntityConfiguration : IEntityTypeConfiguration<TeacherVoiceEntity>
{
    public void Configure(EntityTypeBuilder<TeacherVoiceEntity> builder)
    {
        builder.ToTable("TeacherVoices");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Id).HasMaxLength(64);
        builder.Property(e => e.Name).HasMaxLength(64).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Label).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(32).HasDefaultValue("prebuilt").IsRequired();
        builder.Property(e => e.Gender).HasMaxLength(16).IsRequired();
        builder.Property(e => e.Persona).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Style).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();

        builder.ConfigureAudit<TeacherVoiceEntity, string>();
        builder.ConfigureSoftDelete<TeacherVoiceEntity, string>();

        builder.HasIndex(e => new { e.IsFeatured, e.DisplayOrder });
    }
}
