using EnglishPartner.Modules.Curriculum.Domain.Exercises;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Configurations;

internal sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("exercises");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Instruction).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Level).HasMaxLength(20);
        builder.Property(x => x.Content).HasMaxLength(5000);
        builder.Property(x => x.CorrectAnswer).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Explanation).HasMaxLength(2000);
        builder.Property(x => x.IsAiGenerated).HasDefaultValue(false);

        builder.HasIndex(x => x.LanguageCode);
        builder.HasIndex(x => x.TopicId);
        builder.HasIndex(x => x.VocabularyId);
        builder.HasIndex(x => x.GrammarId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasMany(x => x.Options)
            .WithOne()
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ExerciseOptionConfiguration : IEntityTypeConfiguration<ExerciseOption>
{
    public void Configure(EntityTypeBuilder<ExerciseOption> builder)
    {
        builder.ToTable("exercise_options");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Text).HasMaxLength(500).IsRequired();
    }
}
