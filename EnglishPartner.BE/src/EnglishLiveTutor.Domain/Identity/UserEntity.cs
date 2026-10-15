using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Identity.Entities;

namespace EnglishLiveTutor.Domain.Identity;

public sealed class UserEntity : AggregateRoot<Guid>
{
    private List<UserRoleEntity> _userRoles = new();
    private List<RefreshTokenEntity> _refreshTokens = new();

    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public string PreferredCefrLevel { get; private set; } = "B1";
    public string PreferredVoiceName { get; private set; } = "Aoede";
    public string TimeZoneId { get; private set; } = "Asia/Ho_Chi_Minh";
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public IReadOnlyCollection<UserRoleEntity> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<RefreshTokenEntity> RefreshTokens => _refreshTokens.AsReadOnly();

    private UserEntity()
    {
    }

    public UserEntity(
        string username,
        string email,
        string fullName,
        string passwordHash,
        string preferredCefrLevel = "B1",
        string preferredVoiceName = "Aoede",
        string timeZoneId = "Asia/Ho_Chi_Minh",
        string? avatarUrl = null,
        bool isActive = true,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("Username không được để trống.");
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email không được để trống.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new InvalidOperationException("PasswordHash không được để trống.");

        Username = username.Trim();
        Email = email.Trim();
        NormalizedEmail = NormalizeEmail(email);
        FullName = string.IsNullOrWhiteSpace(fullName) ? Username : fullName.Trim();
        PasswordHash = passwordHash;
        PreferredCefrLevel = string.IsNullOrWhiteSpace(preferredCefrLevel) ? "B1" : preferredCefrLevel.Trim().ToUpperInvariant();
        PreferredVoiceName = string.IsNullOrWhiteSpace(preferredVoiceName) ? "Aoede" : preferredVoiceName.Trim();
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "Asia/Ho_Chi_Minh" : timeZoneId.Trim();
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        IsActive = isActive;
    }

    public static UserEntity Create(
        string username,
        string email,
        string fullName,
        string passwordHash,
        string preferredCefrLevel = "B1",
        string preferredVoiceName = "Aoede",
        string timeZoneId = "Asia/Ho_Chi_Minh",
        string? avatarUrl = null,
        bool isActive = true,
        Guid? id = null) =>
        new(username, email, fullName, passwordHash, preferredCefrLevel, preferredVoiceName, timeZoneId, avatarUrl, isActive, id);

    public static string NormalizeEmail(string email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToUpperInvariant();

    public void UpdateProfile(
        string? fullName,
        string? avatarUrl,
        string? preferredCefrLevel,
        string? preferredVoiceName,
        string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(fullName))
            FullName = fullName.Trim();
        if (avatarUrl is not null)
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        if (!string.IsNullOrWhiteSpace(preferredCefrLevel))
            PreferredCefrLevel = preferredCefrLevel.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(preferredVoiceName))
            PreferredVoiceName = preferredVoiceName.Trim();
        if (!string.IsNullOrWhiteSpace(timeZoneId))
            TimeZoneId = timeZoneId.Trim();
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new InvalidOperationException("Mật khẩu mới không hợp lệ.");
        PasswordHash = newPasswordHash;
    }

    public void RecordLogin(DateTimeOffset loginAtUtc)
    {
        LastLoginAtUtc = loginAtUtc;
    }

    public UserRoleEntity AssignRole(Guid roleId, DateTimeOffset? assignedAtUtc = null)
    {
        var existing = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (existing is not null)
        {
            return existing;
        }

        var userRole = new UserRoleEntity(Id, roleId, assignedAtUtc ?? DateTimeOffset.UtcNow);
        _userRoles.Add(userRole);
        return userRole;
    }

    public void SetRoles(IEnumerable<Guid> roleIds, DateTimeOffset? assignedAtUtc = null)
    {
        _userRoles.Clear();
        foreach (var roleId in roleIds.Distinct())
        {
            AssignRole(roleId, assignedAtUtc);
        }
    }

    public RefreshTokenEntity IssueRefreshToken(
        string token,
        DateTimeOffset expiresAtUtc,
        string? createdByIp = null,
        string? deviceInfo = null,
        Guid? id = null)
    {
        var rt = new RefreshTokenEntity(
            userId: Id,
            token: token,
            expiresAtUtc: expiresAtUtc,
            createdByIp: createdByIp,
            deviceInfo: deviceInfo,
            id: id);

        _refreshTokens.Add(rt);
        return rt;
    }

    public RefreshTokenEntity RotateRefreshToken(
        string currentToken,
        string newToken,
        DateTimeOffset newExpiresAtUtc,
        DateTimeOffset nowUtc,
        string? ipAddress = null,
        string? deviceInfo = null)
    {
        var existing = _refreshTokens.FirstOrDefault(t => t.Token == currentToken)
            ?? throw new InvalidOperationException("Refresh token không tồn tại.");

        if (!existing.IsActive(nowUtc))
            throw new InvalidOperationException("Refresh token đã hết hạn hoặc đã bị thu hồi.");

        existing.Revoke(nowUtc, "Rotated", newToken);
        return IssueRefreshToken(newToken, newExpiresAtUtc, ipAddress, deviceInfo);
    }

    public void RevokeRefreshToken(
        string token,
        DateTimeOffset revokedAtUtc,
        string? reason = "Revoked by user",
        string? replacedByToken = null)
    {
        var existing = _refreshTokens.FirstOrDefault(t => t.Token == token)
            ?? throw new InvalidOperationException("Refresh token không tồn tại.");

        existing.Revoke(revokedAtUtc, reason, replacedByToken);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
