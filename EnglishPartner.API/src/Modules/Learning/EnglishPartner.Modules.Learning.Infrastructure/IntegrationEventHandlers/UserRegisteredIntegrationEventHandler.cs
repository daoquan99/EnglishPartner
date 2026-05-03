using EnglishPartner.Messaging;
using EnglishPartner.Modules.Identity.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Learning.Application.UserProfiles.Commands.InitializeProfile;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Learning.Infrastructure.IntegrationEventHandlers;

internal sealed class UserRegisteredIntegrationEventHandler
    : IdempotentIntegrationEventHandler<UserRegisteredIntegrationEvent, LearningDbContext>
{
    private readonly ISender _sender;
    private readonly ILogger<UserRegisteredIntegrationEventHandler> _logger;

    public UserRegisteredIntegrationEventHandler(
        LearningDbContext dbContext,
        ISender sender,
        TimeProvider timeProvider,
        ILogger<UserRegisteredIntegrationEventHandler> logger)
        : base(dbContext, timeProvider, logger)
    {
        _sender = sender;
        _logger = logger;
    }

    protected override async Task HandleInternalAsync(UserRegisteredIntegrationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Learning Module: Initializing profile for new user {UserId} - {DisplayName}", notification.UserId, notification.DisplayName);

        var command = new InitializeUserLearningProfileCommand(
            notification.UserId,
            notification.Email,
            string.IsNullOrWhiteSpace(notification.DisplayName) ? "Anonymous" : notification.DisplayName,
            "en");

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogError("Failed to initialize profile for user {UserId}: {Error}", notification.UserId, result.Error);
            throw new InvalidOperationException($"Failed to initialize profile: {result.Error.Message}");
        }
    }
}
