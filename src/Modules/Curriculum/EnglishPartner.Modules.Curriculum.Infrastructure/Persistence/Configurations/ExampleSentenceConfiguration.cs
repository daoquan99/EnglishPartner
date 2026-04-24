using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Configurations;

internal sealed class ExampleSentenceConfiguration : IEntityTypeConfiguration<ExampleSentence>
{
    public void Configure(EntityTypeBuilder<ExampleSentence> builder)
    {
        builder.ToTable("example_sentences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Text).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.AudioUrl).HasMaxLength(500);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.Level).HasMaxLength(20);
        builder.Property(x => x.IsAiGenerated).HasDefaultValue(false);

        builder.HasIndex(x => x.LanguageCode);
        builder.HasIndex(x => x.VocabularyId);
        builder.HasIndex(x => x.GrammarId);
        builder.HasIndex(x => x.TopicId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(x => x.ExampleSentenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ExampleSentenceTranslationConfiguration : IEntityTypeConfiguration<ExampleSentenceTranslation>
{
    public void Configure(EntityTypeBuilder<ExampleSentenceTranslation> builder)
    {
        builder.ToTable("example_sentence_translations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.TranslatedText).HasMaxLength(2000).IsRequired();

        // One translation per language per sentence
        builder.HasIndex(x => new { x.ExampleSentenceId, x.LanguageCode }).IsUnique();
    }
}
