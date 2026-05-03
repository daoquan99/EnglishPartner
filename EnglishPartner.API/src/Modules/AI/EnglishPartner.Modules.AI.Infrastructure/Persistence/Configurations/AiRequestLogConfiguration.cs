using EnglishPartner.Modules.AI.Domain.AiRequestLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.AI.Infrastructure.Persistence.Configurations;

internal sealed class AiRequestLogConfiguration : IEntityTypeConfiguration<AiRequestLog>
{
    public void Configure(EntityTypeBuilder<AiRequestLog> builder)
    {
        builder.ToTable("ai_request_logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequestType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.InputText).IsRequired();
        builder.Property(x => x.OutputText).IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.LatencyMs).IsRequired();
        builder.Property(x => x.IsSuccess).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.RequestType);
        builder.HasIndex(x => x.CreatedAt);
    }
}
