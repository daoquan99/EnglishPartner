using EnglishPartner.Modules.Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Seeds default roles and permissions for the Identity module.
/// </summary>
internal static class IdentityDataSeeder
{
    private static readonly Guid LearnerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid AdminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            CreateRole(LearnerRoleId, "Learner", "Default role for all learners"),
            CreateRole(AdminRoleId, "Admin", "System administrator"));
    }

    private static object CreateRole(Guid id, string name, string description) => new
    {
        Id = id,
        Name = name,
        Description = description,
        IsActive = true,
        IsDeleted = false,
        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };
}
