namespace EnglishLiveTutor.Application.Features.Identity.Contracts;

public sealed record RegisterUserRequest(
    string Username,
    string Email,
    string Password,
    string FullName = "",
    string PreferredCefrLevel = "B1",
    string PreferredVoiceName = "Aoede",
    string TimeZoneId = "Asia/Ho_Chi_Minh"
);

public sealed record LoginRequest(
    string UsernameOrEmail,
    string Password,
    string? DeviceInfo = null
);

public sealed record RefreshTokenRequest(
    string RefreshToken,
    string? DeviceInfo = null
);

public sealed record RevokeTokenRequest(
    string RefreshToken,
    string? Reason = "Logged out"
);

public sealed record UserProfileDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string? AvatarUrl,
    string PreferredCefrLevel,
    string PreferredVoiceName,
    string TimeZoneId,
    bool IsActive,
    List<string> Roles,
    List<string> Permissions,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc
);

public sealed record AuthResponseDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    UserProfileDto User
);

public sealed record AssignUserRolesRequest(
    List<string> RoleCodes
);

public sealed record CreateRoleRequest(
    string Code,
    string Name,
    string Description = "",
    List<string>? PermissionCodes = null
);

public sealed record UpdateRolePermissionsRequest(
    string? Name,
    string? Description,
    List<string> PermissionCodes
);

public sealed record CreatePermissionRequest(
    string Code,
    string Name,
    string Module = "General",
    string Description = ""
);

public sealed record PermissionDto(
    Guid Id,
    string Code,
    string Name,
    string Module,
    string Description
);

public sealed record RoleDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    bool IsSystemRole,
    bool IsActive,
    List<string> Permissions
);

public sealed record AdminCreateUserRequest(
    string Username,
    string Email,
    string Password,
    string FullName,
    string PreferredCefrLevel = "B1",
    string PreferredVoiceName = "Aoede",
    bool IsActive = true,
    List<string>? RoleCodes = null
);

public sealed record AdminUpdateUserRequest(
    string Email,
    string FullName,
    string PreferredCefrLevel = "B1",
    string PreferredVoiceName = "Aoede",
    bool IsActive = true,
    string? NewPassword = null,
    List<string>? RoleCodes = null
);

public sealed record AdminUpdateRoleRequest(
    string Name,
    string Description,
    bool IsActive = true,
    List<string>? PermissionCodes = null
);

public sealed record AdminUpdatePermissionRequest(
    string Name,
    string Module,
    string Description
);
