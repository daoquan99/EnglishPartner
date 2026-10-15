namespace EnglishLiveTutor.Application.Features.Identity.Contracts;

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
