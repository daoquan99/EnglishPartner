using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Learning.Services;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.InitializeDailyVocabularyPlan;

public sealed class InitializeDailyVocabularyPlanCommandHandler : ICommandHandler<InitializeDailyVocabularyPlanCommand, DailyVocabularyPlanResponseDto>
{
    private readonly IDailyVocabularyPlanInitializer _initializer;

    public InitializeDailyVocabularyPlanCommandHandler(IDailyVocabularyPlanInitializer initializer)
    {
        _initializer = initializer;
    }

    public Task<DailyVocabularyPlanResponseDto> HandleAsync(InitializeDailyVocabularyPlanCommand command, CancellationToken ct = default)
    {
        return _initializer.InitializeOrGetPlanAsync(command.UserId, ct);
    }
}
