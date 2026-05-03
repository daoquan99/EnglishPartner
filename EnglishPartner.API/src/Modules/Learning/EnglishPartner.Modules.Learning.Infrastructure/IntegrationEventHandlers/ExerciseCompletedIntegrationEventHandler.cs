using EnglishPartner.Messaging;
using EnglishPartner.Modules.Curriculum.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Learning.Application.Abstractions;
using EnglishPartner.Modules.Learning.Domain.UserProfiles;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Learning.Infrastructure.IntegrationEventHandlers;

internal sealed class ExerciseCompletedIntegrationEventHandler : IdempotentIntegrationEventHandler<ExerciseCompletedIntegrationEvent, LearningDbContext>
{
    private readonly ILearningUnitOfWork _unitOfWork;
    private readonly IUserLearningProfileRepository _profileRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ExerciseCompletedIntegrationEventHandler> _logger;

    public ExerciseCompletedIntegrationEventHandler(
        LearningDbContext dbContext,
        ILearningUnitOfWork unitOfWork,
        IUserLearningProfileRepository profileRepository,
        TimeProvider timeProvider,
        ILogger<ExerciseCompletedIntegrationEventHandler> logger) : base(dbContext, timeProvider, logger)
    {
        _unitOfWork = unitOfWork;
        _profileRepository = profileRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task HandleInternalAsync(ExerciseCompletedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (profile is null)
        {
            _logger.LogWarning("UserLearningProfile not found for UserId: {UserId} — auto-creating.", notification.UserId);
            profile = UserLearningProfile.Create(
                userId: notification.UserId,
                email: string.Empty,
                displayName: "Learner");
            _profileRepository.Add(profile);
        }

        profile.AddXp(notification.XpEarned);
        profile.RecordActivity(_timeProvider.GetUtcNow().UtcDateTime);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Added {XpEarned} XP to UserId {UserId} for standalone exercise {ExerciseId}.", 
            notification.XpEarned, notification.UserId, notification.ExerciseId);
    }
}
