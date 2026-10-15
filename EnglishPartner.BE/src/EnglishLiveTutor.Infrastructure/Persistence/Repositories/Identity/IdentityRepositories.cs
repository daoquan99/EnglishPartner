using EnglishLiveTutor.Domain.Identity.Permissions;
using EnglishLiveTutor.Domain.Identity.Roles;
using EnglishLiveTutor.Domain.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.Identity;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<UserEntity?> GetByIdWithRolesAndTokensAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Users
            .Include(u => u.UserRoles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<UserEntity?> GetByEmailOrUsernameWithDetailsAsync(string emailOrUsername, CancellationToken ct = default)
    {
        string trimmed = emailOrUsername.Trim();
        string normEmail = UserEntity.NormalizeEmail(trimmed);

        return await _db.Users
            .Include(u => u.UserRoles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normEmail || u.Username == trimmed, ct);
    }

    public async Task<UserEntity?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        string cleanToken = refreshToken.Trim();
        return await _db.Users
            .Include(u => u.UserRoles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == cleanToken), ct);
    }

    public async Task<bool> ExistsByEmailOrUsernameAsync(string email, string username, CancellationToken ct = default)
    {
        string normEmail = UserEntity.NormalizeEmail(email);
        string cleanUsername = username.Trim();
        return await _db.Users.AnyAsync(u => u.NormalizedEmail == normEmail || u.Username == cleanUsername, ct);
    }

    public Task AddAsync(UserEntity user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        return Task.CompletedTask;
    }
}

public sealed class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _db;

    public RoleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RoleEntity?> GetByIdWithPermissionsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<RoleEntity?> GetByCodeWithPermissionsAsync(string code, CancellationToken ct = default)
    {
        string normCode = code.Trim().ToUpperInvariant();
        return await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Code == normCode, ct);
    }

    public async Task<List<RoleEntity>> GetActiveByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken ct = default)
    {
        if (codes.Count == 0)
            return new List<RoleEntity>();

        var normCodes = codes.Select(c => c.Trim().ToUpperInvariant()).Distinct().ToList();
        return await _db.Roles
            .Where(r => normCodes.Contains(r.Code) && r.IsActive)
            .ToListAsync(ct);
    }

    public async Task<List<RoleEntity>> ListAllWithPermissionsAsync(CancellationToken ct = default)
    {
        return await _db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .OrderBy(r => r.Code)
            .ToListAsync(ct);
    }

    public Task AddAsync(RoleEntity role, CancellationToken ct = default)
    {
        _db.Roles.Add(role);
        return Task.CompletedTask;
    }
}

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly AppDbContext _db;

    public PermissionRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<PermissionEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Permissions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PermissionEntity?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        string normCode = code.Trim().ToLowerInvariant();
        return await _db.Permissions
            .FirstOrDefaultAsync(p => p.Code == normCode, ct);
    }

    public async Task<List<PermissionEntity>> GetByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken ct = default)
    {
        if (codes.Count == 0)
            return new List<PermissionEntity>();

        var normCodes = codes.Select(c => c.Trim().ToLowerInvariant()).Distinct().ToList();
        return await _db.Permissions
            .Where(p => normCodes.Contains(p.Code))
            .ToListAsync(ct);
    }

    public async Task<List<PermissionEntity>> ListAllAsync(CancellationToken ct = default)
    {
        return await _db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .ToListAsync(ct);
    }

    public Task AddAsync(PermissionEntity permission, CancellationToken ct = default)
    {
        _db.Permissions.Add(permission);
        return Task.CompletedTask;
    }
}
