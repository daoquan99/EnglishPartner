using EnglishPartner.Modules.Learning.Domain.UserProfiles;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.UserProfiles.Commands.InitializeProfile;

internal sealed class InitializeUserLearningProfileCommandHandler(
    IUserLearningProfileRepository profileRepository) : ICommandHandler<InitializeUserLearningProfileCommand>
{
    public async Task<Result> Handle(InitializeUserLearningProfileCommand request, CancellationToken cancellationToken)
    {
        var existingProfile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existingProfile is null)
        {
            var profile = UserLearningProfile.Create(request.UserId, request.TargetLanguageCode);
            profileRepository.Add(profile);
        }

        return Result.Success();
    }
}
