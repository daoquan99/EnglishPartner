using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Entities;
using EnglishLiveTutor.Domain.Identity.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    private static readonly object _inMemoryLock = new();

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserEntity?> GetByIdWithRolesAndTokensAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<UserEntity?> GetByEmailOrUsernameWithDetailsAsync(string emailOrUsername, CancellationToken ct = default)
    {
        string trimmed = emailOrUsername.Trim();
        string normEmail = UserEntity.NormalizeEmail(trimmed);

        return await _db.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normEmail || u.Username == trimmed, ct);
    }

    public async Task<UserEntity?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        string cleanToken = refreshToken.Trim();
        return await _db.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .Include(u => u.RefreshTokens.Where(rt => rt.Token == cleanToken))
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

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> RevokeAllActiveRefreshTokensAsync(Guid userId, DateTimeOffset revokedAtUtc, string reason = "Reuse detected", CancellationToken ct = default)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null && rt.ExpiresAtUtc > revokedAtUtc)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.Revoke(revokedAtUtc, reason);
        }

        return activeTokens.Count;
    }

    public async Task PurgeExpiredOrRevokedRefreshTokensAsync(Guid userId, DateTimeOffset nowUtc, CancellationToken ct = default)
    {
        var cutoff = nowUtc.AddDays(-30);
        var oldTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && (rt.ExpiresAtUtc <= nowUtc || (rt.RevokedAtUtc != null && rt.RevokedAtUtc <= cutoff)))
            .ToListAsync(ct);

        if (oldTokens.Count > 0)
        {
            _db.RefreshTokens.RemoveRange(oldTokens);
        }
    }

    public async Task<bool> RotateRefreshTokenConditionallyAsync(
        Guid oldTokenId,
        RefreshTokenEntity newToken,
        DateTimeOffset nowUtc,
        CancellationToken ct = default)
    {
        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            lock (_inMemoryLock)
            {
                var existing = _db.RefreshTokens.FirstOrDefault(rt => rt.Id == oldTokenId && rt.RevokedAtUtc == null);
                if (existing is null)
                {
                    return false;
                }

                existing.Revoke(nowUtc, "Rotated", newToken.Token);
                _db.RefreshTokens.Add(newToken);
                _db.SaveChanges();
                return true;
            }
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                int rows = await _db.RefreshTokens
                    .Where(rt => rt.Id == oldTokenId && rt.RevokedAtUtc == null)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(rt => rt.RevokedAtUtc, nowUtc)
                        .SetProperty(rt => rt.RevokedReason, "Rotated")
                        .SetProperty(rt => rt.ReplacedByToken, newToken.Token), ct);

                if (rows != 1)
                {
                    await tx.RollbackAsync(ct);
                    return false;
                }

                var trackedOld = _db.ChangeTracker.Entries<RefreshTokenEntity>()
                    .FirstOrDefault(e => e.Entity.Id == oldTokenId);
                if (trackedOld != null)
                {
                    trackedOld.State = EntityState.Detached;
                }

                await _db.RefreshTokens.AddAsync(newToken, ct);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return true;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task<RefreshTokenEntity?> GetRefreshTokenByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(rt => rt.Id == id, ct);
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

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly AppDbContext _db;

    public PermissionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PermissionEntity?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        string normCode = code.Trim().ToLowerInvariant();
        return await _db.Permissions
            .FirstOrDefaultAsync(p => p.Code == normCode, ct);
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

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
