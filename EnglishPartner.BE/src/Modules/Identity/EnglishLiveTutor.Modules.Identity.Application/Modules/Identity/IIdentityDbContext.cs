using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Modules.Identity;

public interface IIdentityDbContext : IModuleDbContext
{
    DbSet<UserEntity> Users { get; }
    DbSet<RoleEntity> Roles { get; }
    DbSet<PermissionEntity> Permissions { get; }
    DbSet<UserRoleEntity> UserRoles { get; }
    DbSet<RolePermissionEntity> RolePermissions { get; }
    DbSet<RefreshTokenEntity> RefreshTokens { get; }
}
