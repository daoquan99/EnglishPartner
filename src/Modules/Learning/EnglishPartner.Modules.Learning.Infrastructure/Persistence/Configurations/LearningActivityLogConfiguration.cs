using EnglishPartner.Modules.Learning.Domain.StudySessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Configurations;

internal sealed class LearningActivityLogConfiguration : IEntityTypeConfiguration<LearningActivityLog>
{
    public void Configure(EntityTypeBuilder<LearningActivityLog> builder)
    {
        builder.ToTable("activity_logs");
        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.ItemId);
        builder.HasIndex(x => x.Timestamp);
    }
}
