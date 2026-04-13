using EnglishPartner.Modules.Identity.Domain.Users;
using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        builder.ToTable("user_external_logins");

        builder.HasKey(x => new { x.LoginProvider, x.ProviderKey });

        builder.Property(x => x.LoginProvider)
               .HasMaxLength(128)
               .IsRequired();

        builder.Property(x => x.ProviderKey)
               .HasMaxLength(256)
               .IsRequired();

        builder.Property(x => x.ProviderDisplayName)
               .HasMaxLength(256);

        // Foreign Key is already inferred via User having IReadOnlyList<UserExternalLogin>
        // But for absolute clarity we explicitly define the relationship
        builder.HasOne<User>()
               .WithMany(u => u.ExternalLogins)
               .HasForeignKey(x => x.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);
    }
}
