using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, bool>
{
    private readonly IIdentityDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public DeleteUserCommandHandler(IIdentityDbContext db, IUnitOfWork uow, ICurrentUser currentUser)
    {
        _db = db;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(DeleteUserCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null)
            return false;

        user.SoftDelete(DateTimeOffset.UtcNow, _currentUser.UserIdOrSystem);
        await _uow.CommitChangesAsync(ct);
        return true;
    }
}
