using EnglishPartner.Modules.Curriculum.Domain.Vocabularies;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Entities;
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
        builder.Property(x => x.ExampleSentence).HasMaxLength(1000);
        builder.Property(x => x.ExampleTranslation).HasMaxLength(1000);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);
        builder.Property(x => x.AudioUrl).HasMaxLength(500);
        builder.Property(x => x.Level).HasMaxLength(20);

        builder.HasIndex(x => new { x.Word, x.LanguageCode });
        builder.HasIndex(x => x.LanguageCode);
        builder.HasIndex(x => x.TopicId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasMany(x => x.Synonyms)
            .WithOne()
            .HasForeignKey(x => x.VocabularyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class VocabularySynonymConfiguration : IEntityTypeConfiguration<VocabularySynonym>
{
    public void Configure(EntityTypeBuilder<VocabularySynonym> builder)
    {
        builder.ToTable("vocabulary_synonyms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SynonymWord).HasMaxLength(200).IsRequired();
    }
}
