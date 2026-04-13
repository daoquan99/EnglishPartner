using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.UserProfiles.Commands.InitializeProfile;

public sealed record InitializeUserLearningProfileCommand(
    Guid UserId,
    string TargetLanguageCode) : ICommand;
