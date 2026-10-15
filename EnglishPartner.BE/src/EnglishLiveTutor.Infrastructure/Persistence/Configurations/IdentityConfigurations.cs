using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishLiveTutor.Infrastructure.Persistence.Configurations;

public sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Username).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.FullName).HasMaxLength(160).IsRequired();
        builder.Property(e => e.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(e => e.AvatarUrl).HasMaxLength(512);
        builder.Property(e => e.PreferredCefrLevel).HasMaxLength(8).HasDefaultValue("B1").IsRequired();
        builder.Property(e => e.PreferredVoiceName).HasMaxLength(64).HasDefaultValue("Aoede").IsRequired();
        builder.Property(e => e.TimeZoneId).HasMaxLength(64).HasDefaultValue("Asia/Ho_Chi_Minh").IsRequired();

        builder.ConfigureAudit<UserEntity, Guid>();
        builder.ConfigureSoftDelete<UserEntity, Guid>();

        builder.HasIndex(e => e.Username).IsUnique();
        builder.HasIndex(e => e.NormalizedEmail).IsUnique();

        builder.HasMany(e => e.UserRoles)
            .WithOne()
            .HasForeignKey(ur => ur.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.UserRoles)
            .HasField("_userRoles")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.RefreshTokens)
            .WithOne()
            .HasForeignKey(rt => rt.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.RefreshTokens)
            .HasField("_refreshTokens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class RoleEntityConfiguration : IEntityTypeConfiguration<RoleEntity>
{
    public void Configure(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Code).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.ConfigureAudit<RoleEntity, Guid>();
        builder.ConfigureSoftDelete<RoleEntity, Guid>();

        builder.HasIndex(e => e.Code).IsUnique();

        builder.HasMany(e => e.RolePermissions)
            .WithOne()
            .HasForeignKey(rp => rp.RoleId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.RolePermissions)
            .HasField("_rolePermissions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PermissionEntityConfiguration : IEntityTypeConfiguration<PermissionEntity>
{
    public void Configure(EntityTypeBuilder<PermissionEntity> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Code).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(160).IsRequired();
        builder.Property(e => e.Module).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.ConfigureAudit<PermissionEntity, Guid>();
        builder.ConfigureSoftDelete<PermissionEntity, Guid>();

        builder.HasIndex(e => e.Code).IsUnique();
    }
}

public sealed class UserRoleEntityConfiguration : IEntityTypeConfiguration<UserRoleEntity>
{
    public void Configure(EntityTypeBuilder<UserRoleEntity> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.ConfigureAudit<UserRoleEntity, Guid>();

        builder.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
    }
}

public sealed class RolePermissionEntityConfiguration : IEntityTypeConfiguration<RolePermissionEntity>
{
    public void Configure(EntityTypeBuilder<RolePermissionEntity> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.ConfigureAudit<RolePermissionEntity, Guid>();

        builder.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
    }
}

public sealed class RefreshTokenEntityConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Token).HasMaxLength(256).IsRequired();
        builder.Property(e => e.RevokedReason).HasMaxLength(256);
        builder.Property(e => e.ReplacedByToken).HasMaxLength(256);
        builder.Property(e => e.CreatedByIp).HasMaxLength(64);
        builder.Property(e => e.DeviceInfo).HasMaxLength(256);
        builder.Ignore(e => e.IsRevoked);

        builder.ConfigureAudit<RefreshTokenEntity, Guid>();

        builder.HasIndex(e => e.Token).IsUnique();
        builder.HasIndex(e => e.UserId);
    }
}
