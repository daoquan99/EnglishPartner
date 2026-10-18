using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.UpdateLearningSettings;

public sealed record UpdateLearningSettingsCommand(
    string UserId,
    UpdateLearningSettingsRequest Request
) : ICommand<UserLearningSettingsDto>;
