using EnglishPartner.Modules.Learning.Domain;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.Admin.Commands.AdjustUserXp;

internal sealed class AdjustUserXpCommandHandler(
    IUserLearningProfileRepository profileRepository) : ICommandHandler<AdjustUserXpCommand, AdjustXpResult>
{
    public async Task<Result<AdjustXpResult>> Handle(AdjustUserXpCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount == 0)
            return Result.Failure<AdjustXpResult>(
                new Error("Learning.Xp.InvalidAmount", "Amount must be non-zero."));

        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
            return Result.Failure<AdjustXpResult>(LearningErrors.Profile.NotFound(request.UserId));

        if (request.Amount > 0)
            profile.AddXp(request.Amount);
        else
            profile.SubtractXp(Math.Abs(request.Amount));

        profileRepository.Update(profile);
        return new AdjustXpResult(profile.TotalXp, profile.CurrentLevel);
    }
}
