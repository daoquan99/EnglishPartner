using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.NativeLanguage).HasMaxLength(10);
        builder.Property(p => p.TargetLanguage).HasMaxLength(10).HasDefaultValue("en");
        builder.Property(p => p.ProficiencyLevel).HasMaxLength(20);
        builder.Property(p => p.LearningGoal).HasMaxLength(50);
        builder.Property(p => p.AvatarUrl).HasMaxLength(500);

        builder.HasIndex(p => p.UserId).IsUnique();
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.HasKey(ur => ur.Id);
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
    }
}

internal sealed class StreakConfiguration : IEntityTypeConfiguration<Streak>
{
    public void Configure(EntityTypeBuilder<Streak> builder)
    {
        builder.ToTable("streaks");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.UserId).IsUnique();
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token).HasMaxLength(512).IsRequired();
        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.UserId);

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsValid);
    }
}

internal sealed class OnboardingAnswerConfiguration : IEntityTypeConfiguration<OnboardingAnswer>
{
    public void Configure(EntityTypeBuilder<OnboardingAnswer> builder)
    {
        builder.ToTable("onboarding_answers");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.QuestionKey).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Answer).HasMaxLength(500).IsRequired();

        builder.HasIndex(a => a.UserId);
    }
}
