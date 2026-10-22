using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations;

public sealed class LessonRolePlaySessionEntityConfiguration : IEntityTypeConfiguration<LessonRolePlaySessionEntity>
{
    public void Configure(EntityTypeBuilder<LessonRolePlaySessionEntity> builder)
    {
        builder.ToTable("LessonRolePlaySessions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Scenario).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.AiRole).HasMaxLength(100).IsRequired();
        builder.Property(e => e.InitialGreeting).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.MaxTurns).IsRequired();
        builder.Property(e => e.CurrentTurnIndex).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(32).IsRequired();
        builder.Property(e => e.EndedAtUtc);
        builder.Property(e => e.ClaimedUntilUtc);
        builder.Property(e => e.Version).IsRequired();
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.ConfigureAudit<LessonRolePlaySessionEntity, Guid>();
        builder.ConfigureSoftDelete<LessonRolePlaySessionEntity, Guid>();

        builder.HasIndex(e => new { e.UserId, e.Status });
        builder.HasIndex(e => e.CreatedAtUtc);

        builder.HasMany(e => e.Turns)
            .WithOne()
            .HasForeignKey(t => t.SessionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Turns)
            .HasField("_turns")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class LessonRolePlayTurnEntityConfiguration : IEntityTypeConfiguration<LessonRolePlayTurnEntity>
{
    public void Configure(EntityTypeBuilder<LessonRolePlayTurnEntity> builder)
    {
        builder.ToTable("LessonRolePlayTurns");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.SessionId).IsRequired();
        builder.Property(e => e.SequenceIndex).IsRequired();
        builder.Property(e => e.TurnIndex).IsRequired();
        builder.Property(e => e.Role).HasMaxLength(32).IsRequired();
        builder.Property(e => e.Message).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.AudioAssetId);
        builder.Property(e => e.CreatedAtUtc).IsRequired();

        builder.ConfigureAudit<LessonRolePlayTurnEntity, Guid>();

        builder.HasIndex(e => new { e.SessionId, e.SequenceIndex }).IsUnique();
        builder.HasIndex(e => new { e.SessionId, e.TurnIndex, e.Role }).IsUnique();
    }
}
