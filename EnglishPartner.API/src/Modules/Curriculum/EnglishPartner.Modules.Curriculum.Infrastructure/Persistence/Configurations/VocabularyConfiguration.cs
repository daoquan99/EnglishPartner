using EnglishPartner.Modules.Curriculum.Domain.Vocabularies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Configurations;

internal sealed class VocabularyConfiguration : IEntityTypeConfiguration<Vocabulary>
{
    public void Configure(EntityTypeBuilder<Vocabulary> builder)
    {
        builder.ToTable("vocabularies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Word).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Definition).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Pronunciation).HasMaxLength(200);
        builder.Property(x => x.PhoneticTranscription).HasMaxLength(200);
        builder.Property(x => x.PartOfSpeech).HasMaxLength(50);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);
        builder.Property(x => x.AudioUrl).HasMaxLength(500);
        builder.Property(x => x.Level).HasMaxLength(20);
        builder.Property(x => x.IsAiGenerated).HasDefaultValue(false);

        // Synonyms stored as a PostgreSQL text[] array — no join table needed
        builder.Property(x => x.Synonyms)
            .HasColumnType("text[]");

        builder.HasIndex(x => new { x.Word, x.LanguageCode });
        builder.HasIndex(x => x.LanguageCode);
        builder.HasIndex(x => x.TopicId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
