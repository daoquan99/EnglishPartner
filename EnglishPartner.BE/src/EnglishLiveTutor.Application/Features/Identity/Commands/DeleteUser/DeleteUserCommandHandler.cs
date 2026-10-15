using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, bool>
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly ICurrentUser _currentUser;

    public DeleteUserCommandHandler(IAppDbContextFactory dbFactory, ICurrentUser currentUser)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(DeleteUserCommand command, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null)
            return false;

        user.SoftDelete(DateTimeOffset.UtcNow, _currentUser.UserIdOrSystem);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
