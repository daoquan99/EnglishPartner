using EnglishPartner.Modules.Curriculum.Domain.Grammars;
using EnglishPartner.Modules.Curriculum.Domain.Grammars.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Configurations;

internal sealed class GrammarConfiguration : IEntityTypeConfiguration<Grammar>
{
    public void Configure(EntityTypeBuilder<Grammar> builder)
    {
        builder.ToTable("grammars");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Level).HasMaxLength(20);
        builder.Property(x => x.Explanation).HasMaxLength(5000);
        builder.Property(x => x.Formula).HasMaxLength(500);
        builder.Property(x => x.IsAiGenerated).HasDefaultValue(false);

        builder.HasIndex(x => x.LanguageCode);
        builder.HasIndex(x => x.TopicId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasMany(x => x.Examples)
            .WithOne()
            .HasForeignKey(x => x.GrammarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class GrammarExampleConfiguration : IEntityTypeConfiguration<GrammarExample>
{
    public void Configure(EntityTypeBuilder<GrammarExample> builder)
    {
        builder.ToTable("grammar_examples");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sentence).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(x => x.GrammarExampleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class GrammarExampleTranslationConfiguration : IEntityTypeConfiguration<GrammarExampleTranslation>
{
    public void Configure(EntityTypeBuilder<GrammarExampleTranslation> builder)
    {
        builder.ToTable("grammar_example_translations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.TranslatedText).HasMaxLength(2000).IsRequired();

        // One translation per language per example
        builder.HasIndex(x => new { x.GrammarExampleId, x.LanguageCode }).IsUnique();
    }
}
