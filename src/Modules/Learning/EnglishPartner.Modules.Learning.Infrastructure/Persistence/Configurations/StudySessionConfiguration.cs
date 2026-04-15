using EnglishPartner.Modules.Learning.Domain.StudySessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Configurations;

internal sealed class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
{
    public void Configure(EntityTypeBuilder<StudySession> builder)
    {
        builder.ToTable("study_sessions");
        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.Status });          // AbandonStaleSessionsAsync filter
        builder.HasIndex(x => new { x.UserId, x.StartTime });       // GetUserSessionsAsync ORDER BY
        builder.Property(x => x.SessionTitle).HasMaxLength(200).IsRequired();

    }
}
