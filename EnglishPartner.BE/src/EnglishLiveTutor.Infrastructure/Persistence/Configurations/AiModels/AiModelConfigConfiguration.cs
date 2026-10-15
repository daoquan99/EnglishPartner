using EnglishLiveTutor.Domain.AiCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations.AiModels;

public sealed class AiModelConfigEntityConfiguration : IEntityTypeConfiguration<AiModelConfigEntity>
{
    public void Configure(EntityTypeBuilder<AiModelConfigEntity> builder)
    {
        builder.ToTable("AiModelCatalog");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.DisplayName).HasMaxLength(128).IsRequired();
        builder.Property(e => e.ModelCode).HasMaxLength(128).IsRequired();
        builder.Property(e => e.RoutedModelCode).HasMaxLength(128);
        builder.Property(e => e.CapabilityNote).HasMaxLength(500);

        builder.ConfigureAudit<AiModelConfigEntity, Guid>();
        builder.ConfigureSoftDelete<AiModelConfigEntity, Guid>();

        builder.HasIndex(e => e.ModelCode).IsUnique();
    }
}
