using EnglishLiveTutor.Application.Common.Cqrs;
using EnglishLiveTutor.Application.Common.Identity;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.Identity.Users;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId) : ICommand<bool>;

public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, bool>
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public DeleteUserCommandHandler(
        IUserRepository userRepo,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        TimeProvider? timeProvider = null)
    {
        _userRepo = userRepo;
        _uow = uow;
        _currentUser = currentUser;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<bool> HandleAsync(DeleteUserCommand command, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(command.UserId, ct);
        if (user is null)
            return false;

        user.SoftDelete(_timeProvider.GetUtcNow(), _currentUser.UserIdOrSystem);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
