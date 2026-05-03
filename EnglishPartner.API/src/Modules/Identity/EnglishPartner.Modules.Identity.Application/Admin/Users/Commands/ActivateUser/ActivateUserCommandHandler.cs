using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.ActivateUser;

internal sealed class ActivateUserCommandHandler(
    IUserRepository userRepository) : ICommandHandler<ActivateUserCommand>
{
    public async Task<Result> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(IdentityErrors.User.NotFoundById(request.UserId));

        user.Activate();
        userRepository.Update(user);
        return Result.Success();
    }
}
