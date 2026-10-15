using EnglishLiveTutor.Domain.Vocabulary.GenerationRuns;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations.Vocabulary;

public sealed class VocabularyEntityConfiguration : IEntityTypeConfiguration<VocabularyEntity>
{
    public void Configure(EntityTypeBuilder<VocabularyEntity> builder)
    {
        builder.ToTable("Vocabularies");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Word).HasMaxLength(160).IsRequired();
        builder.Property(e => e.NormalizedWord).HasMaxLength(160).IsRequired();
        builder.Property(e => e.LanguageCode).HasMaxLength(16).HasDefaultValue("en").IsRequired();
        builder.Property(e => e.SourceType).HasMaxLength(32).HasDefaultValue("Seeded").IsRequired();
        builder.Property(e => e.SourceModelId).HasMaxLength(64);
        builder.Property(e => e.TopicCategory).HasMaxLength(128);

        builder.ConfigureAudit<VocabularyEntity, Guid>(
            createdAtColumnName: "CreatedAt",
            updatedAtColumnName: "UpdatedAt");
        builder.ConfigureSoftDelete<VocabularyEntity, Guid>();

        builder.HasIndex(e => new { e.NormalizedWord, e.LanguageCode });

        builder.HasMany(e => e.Senses)
            .WithOne()
            .HasForeignKey(s => s.VocabularyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Senses)
            .HasField("_senses")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Pronunciations)
            .WithOne()
            .HasForeignKey(p => p.VocabularyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Pronunciations)
            .HasField("_pronunciations")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.WordFamilies)
            .WithOne()
            .HasForeignKey(w => w.VocabularyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.WordFamilies)
            .HasField("_wordFamilies")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class VocabularySenseEntityConfiguration : IEntityTypeConfiguration<VocabularySenseEntity>
{
    public void Configure(EntityTypeBuilder<VocabularySenseEntity> builder)
    {
        builder.ToTable("VocabularySenses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.PartOfSpeech).HasMaxLength(64).IsRequired();
        builder.Property(e => e.DefinitionEn).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.MeaningVi).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.DescriptionEn).HasMaxLength(1500);
        builder.Property(e => e.CefrLevel).HasMaxLength(8).HasDefaultValue("B1").IsRequired();

        builder.ConfigureAudit<VocabularySenseEntity, Guid>();

        builder.HasIndex(e => new { e.VocabularyId, e.DisplayOrder });

        builder.HasMany(e => e.Relations)
            .WithOne()
            .HasForeignKey(r => r.SenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Relations)
            .HasField("_relations")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Collocations)
            .WithOne()
            .HasForeignKey(c => c.SenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Collocations)
            .HasField("_collocations")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Examples)
            .WithOne()
            .HasForeignKey(x => x.SenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Examples)
            .HasField("_examples")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.GrammarPatterns)
            .WithOne()
            .HasForeignKey(g => g.SenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.GrammarPatterns)
            .HasField("_grammarPatterns")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class VocabularyPronunciationEntityConfiguration : IEntityTypeConfiguration<VocabularyPronunciationEntity>
{
    public void Configure(EntityTypeBuilder<VocabularyPronunciationEntity> builder)
    {
        builder.ToTable("VocabularyPronunciations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Accent).HasMaxLength(16).HasDefaultValue("US").IsRequired();
        builder.Property(e => e.Ipa).HasMaxLength(160).IsRequired();
        builder.Property(e => e.AudioUrl).HasMaxLength(512);
        builder.Property(e => e.AudioSource).HasMaxLength(64).HasDefaultValue("BrowserTTS").IsRequired();
        builder.Property(e => e.StressPattern).HasMaxLength(64);

        builder.ConfigureAudit<VocabularyPronunciationEntity, Guid>();
    }
}

public sealed class VocabularyRelationEntityConfiguration : IEntityTypeConfiguration<VocabularyRelationEntity>
{
    public void Configure(EntityTypeBuilder<VocabularyRelationEntity> builder)
    {
        builder.ToTable("VocabularyRelations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.RelationType).HasMaxLength(32).IsRequired();
        builder.Property(e => e.RelatedWord).HasMaxLength(160).IsRequired();

        builder.ConfigureAudit<VocabularyRelationEntity, Guid>();
    }
}

public sealed class VocabularyWordFamilyEntityConfiguration : IEntityTypeConfiguration<VocabularyWordFamilyEntity>
{
    public void Configure(EntityTypeBuilder<VocabularyWordFamilyEntity> builder)
    {
        builder.ToTable("VocabularyWordFamilies");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.RelatedWord).HasMaxLength(160).IsRequired();
        builder.Property(e => e.PartOfSpeech).HasMaxLength(64).IsRequired();

        builder.ConfigureAudit<VocabularyWordFamilyEntity, Guid>();
    }
}

public sealed class VocabularyCollocationEntityConfiguration : IEntityTypeConfiguration<VocabularyCollocationEntity>
{
    public void Configure(EntityTypeBuilder<VocabularyCollocationEntity> builder)
    {
        builder.ToTable("VocabularyCollocations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.PhraseEn).HasMaxLength(300).IsRequired();
        builder.Property(e => e.MeaningVi).HasMaxLength(300);

        builder.ConfigureAudit<VocabularyCollocationEntity, Guid>();
    }
}

public sealed class VocabularyExampleEntityConfiguration : IEntityTypeConfiguration<VocabularyExampleEntity>
{
    public void Configure(EntityTypeBuilder<VocabularyExampleEntity> builder)
    {
        builder.ToTable("VocabularyExamples");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.SentenceEn).HasMaxLength(600).IsRequired();
        builder.Property(e => e.SentenceVi).HasMaxLength(600);
        builder.Property(e => e.AudioUrl).HasMaxLength(512);
        builder.Property(e => e.CefrLevel).HasMaxLength(8);

        builder.ConfigureAudit<VocabularyExampleEntity, Guid>();
    }
}

public sealed class VocabularyGrammarPatternEntityConfiguration : IEntityTypeConfiguration<VocabularyGrammarPatternEntity>
{
    public void Configure(EntityTypeBuilder<VocabularyGrammarPatternEntity> builder)
    {
        builder.ToTable("VocabularyGrammarPatterns");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Pattern).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Explanation).HasMaxLength(600);
        builder.Property(e => e.Example).HasMaxLength(600);

        builder.ConfigureAudit<VocabularyGrammarPatternEntity, Guid>();
    }
}

public sealed class VocabularyGenerationAuditEntityConfiguration : IEntityTypeConfiguration<VocabularyGenerationAuditEntity>
{
    public void Configure(EntityTypeBuilder<VocabularyGenerationAuditEntity> builder)
    {
        builder.ToTable("VocabularyGenerationAudits");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.ModelCode).HasMaxLength(128).IsRequired();
        builder.Property(e => e.PromptVersion).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(32).IsRequired();

        builder.ConfigureAudit<VocabularyGenerationAuditEntity, Guid>(
            createdAtColumnName: "CreatedAt",
            updatedAtColumnName: "UpdatedAtUtc");
        builder.ConfigureSoftDelete<VocabularyGenerationAuditEntity, Guid>();
    }
}
