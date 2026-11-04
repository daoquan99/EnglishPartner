using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Modules.Identity;
using EnglishLiveTutor.Domain.Identity.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AssignUserRoles;

public sealed class AssignUserRolesCommandHandler : ICommandHandler<AssignUserRolesCommand, UserProfileDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IIdentityDbContext _db;
    private readonly IUnitOfWork _uow;

    public AssignUserRolesCommandHandler(
        IUserRepository userRepo,
        IIdentityDbContext db,
        IUnitOfWork uow)
    {
        _userRepo = userRepo;
        _db = db;
        _uow = uow;
    }

    public async Task<UserProfileDto> HandleAsync(AssignUserRolesCommand command, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdWithRolesAndTokensAsync(command.UserId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy người dùng.");

        var normalizedCodes = (command.Request.RoleCodes ?? new List<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        var roles = await _db.Roles
            .AsNoTracking()
            .Where(r => normalizedCodes.Contains(r.Code) && r.IsActive)
            .ToListAsync(ct);

        user.SetRoles(roles.Select(r => r.Id));
        await _uow.CommitChangesAsync(ct);

        var (roleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(_db, user.Id, ct);
        return IdentityProjectionHelper.MapToProfileDto(user, roleCodes, permissionCodes);
    }
}
