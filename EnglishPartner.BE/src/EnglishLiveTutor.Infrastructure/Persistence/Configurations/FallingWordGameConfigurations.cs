using EnglishLiveTutor.Domain.VocabularyGames;
using EnglishLiveTutor.Domain.VocabularyGames.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations;

public sealed class FallingWordGameSessionEntityConfiguration : IEntityTypeConfiguration<FallingWordGameSessionEntity>
{
    public void Configure(EntityTypeBuilder<FallingWordGameSessionEntity> builder)
    {
        builder.ToTable("FallingWordGameSessions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Source).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.PlayFilter).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(e => e.CefrLevel).HasMaxLength(8);
        builder.Property(e => e.LearningDate).HasMaxLength(16);
        builder.Property(e => e.TimeZoneId).HasMaxLength(64);

        builder.Property(e => e.TimeLimitSeconds).IsRequired();
        builder.Property(e => e.RequestedCount).IsRequired();
        builder.Property(e => e.ActualCount).IsRequired();

        builder.Property(e => e.CorrectCount).IsRequired();
        builder.Property(e => e.IncorrectCount).IsRequired();
        builder.Property(e => e.TimedOutCount).IsRequired();
        builder.Property(e => e.AbandonedCount).IsRequired();
        builder.Property(e => e.BestStreak).IsRequired();
        builder.Property(e => e.CurrentStreak).IsRequired();

        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.ConfigureAudit<FallingWordGameSessionEntity, Guid>();
        builder.ConfigureSoftDelete<FallingWordGameSessionEntity, Guid>();

        builder.HasIndex(e => new { e.UserId, e.Status });
        builder.HasIndex(e => new { e.UserId, e.CreatedAtUtc });

        builder.HasMany(e => e.Rounds)
            .WithOne()
            .HasForeignKey(r => r.SessionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Rounds)
            .HasField("_rounds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class FallingWordGameRoundEntityConfiguration : IEntityTypeConfiguration<FallingWordGameRoundEntity>
{
    public void Configure(EntityTypeBuilder<FallingWordGameRoundEntity> builder)
    {
        builder.ToTable("FallingWordGameRounds");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.SessionId).IsRequired();
        builder.Property(e => e.RoundOrder).IsRequired();
        builder.Property(e => e.VocabularyId).IsRequired();
        builder.Property(e => e.SenseId).IsRequired();

        builder.Property(e => e.TargetWordSnapshot).HasMaxLength(256).IsRequired();
        builder.Property(e => e.NormalizedWordSnapshot).HasMaxLength(256).IsRequired();
        builder.Property(e => e.MeaningViSnapshot).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.CefrLevelSnapshot).HasMaxLength(8);
        builder.Property(e => e.MaskedPattern).HasMaxLength(500).IsRequired();
        builder.Property(e => e.LetterCount).IsRequired();

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.SubmittedText).HasMaxLength(256);
        builder.Property(e => e.FailureReason).HasMaxLength(64);

        builder.ConfigureAudit<FallingWordGameRoundEntity, Guid>();

        builder.HasIndex(e => new { e.SessionId, e.RoundOrder }).IsUnique();
        builder.HasIndex(e => new { e.SenseId, e.Status });
        builder.HasIndex(e => new { e.Status, e.ExpiresAtUtc });
    }
}
