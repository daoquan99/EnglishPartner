using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Profile.Commands.UpdateMyProfile;

internal sealed class UpdateMyProfileCommandHandler(
    IUserRepository userRepository,
    IUserProfileRepository profileRepository) : ICommandHandler<UpdateMyProfileCommand>
{
    public async Task<Result> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(IdentityErrors.User.NotFoundById(request.UserId));

        if (request.DisplayName is not null)
            user.UpdateProfile(request.DisplayName);

        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var isNew = profile is null;

        if (isNew)
            profile = UserProfile.Create(request.UserId);

        profile!.Update(
            nativeLanguage: request.NativeLanguage,
            targetLanguage: request.TargetLanguage,
            proficiencyLevel: request.ProficiencyLevel,
            learningGoal: request.LearningGoal,
            dailyGoalMinutes: request.DailyGoalMinutes,
            avatarUrl: request.AvatarUrl,
            dateOfBirth: request.DateOfBirth,
            morningReminderHour: request.MorningReminderHour,
            eveningReminderHour: request.EveningReminderHour,
            utcOffsetHours: request.UtcOffsetHours);

        // Raise domain event via User (AggregateRoot) — UserProfile is an Entity and cannot raise events.
        user.RaiseProfileUpdated(
            profile.DailyGoalMinutes,
            profile.MorningReminderHour,
            profile.EveningReminderHour,
            profile.UtcOffsetHours);

        if (isNew)
            profileRepository.Add(profile);
        else
            profileRepository.Update(profile);

        return Result.Success();
    }
}
