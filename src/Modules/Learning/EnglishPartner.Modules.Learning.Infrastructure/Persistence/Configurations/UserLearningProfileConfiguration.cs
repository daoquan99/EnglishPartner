using EnglishPartner.Modules.Learning.Domain.UserProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Configurations;

internal sealed class UserLearningProfileConfiguration : IEntityTypeConfiguration<UserLearningProfile>
{
    public void Configure(EntityTypeBuilder<UserLearningProfile> builder)
    {
        builder.ToTable("user_learning_profiles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique(); // One profile per user
        
        builder.Property(x => x.TargetLanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.CurrentLevel).HasMaxLength(20).IsRequired();
    }
}
