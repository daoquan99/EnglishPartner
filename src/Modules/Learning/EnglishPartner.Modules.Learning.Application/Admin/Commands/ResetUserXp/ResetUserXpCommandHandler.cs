using EnglishPartner.Modules.Learning.Domain;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.Admin.Commands.ResetUserXp;

internal sealed class ResetUserXpCommandHandler(
    IUserLearningProfileRepository profileRepository) : ICommandHandler<ResetUserXpCommand>
{
    public async Task<Result> Handle(ResetUserXpCommand request, CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
            return Result.Failure(LearningErrors.Profile.NotFound(request.UserId));

        profile.ResetXp();
        profileRepository.Update(profile);
        return Result.Success();
    }
}
