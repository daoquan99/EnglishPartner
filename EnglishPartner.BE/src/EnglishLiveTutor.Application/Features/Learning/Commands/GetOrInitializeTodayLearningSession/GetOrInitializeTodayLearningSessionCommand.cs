using EnglishLiveTutor.Application.Common.Cqrs;
using EnglishLiveTutor.Application.Common.Identity;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Learning.Commands.InitializeDailyVocabularyPlan;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.GetOrInitializeTodayLearningSession;

/// <summary>
/// Legacy compatibility command for GET /api/learning/flashcards/today.
/// Explicitly named as a Command because it initializes today's plan if not yet created.
/// Pure read callers should use <see cref="Queries.GetDailyVocabularyPlan.GetDailyVocabularyPlanQuery"/>.
/// </summary>
public sealed record GetOrInitializeTodayLearningSessionCommand(
    string UserId,
    string? TimeZoneId,
    int? Target
) : ICommand<DailyVocabularyPlanResponseDto>;

public sealed class GetOrInitializeTodayLearningSessionCommandHandler
    : ICommandHandler<GetOrInitializeTodayLearningSessionCommand, DailyVocabularyPlanResponseDto>
{
    private readonly ICommandHandler<InitializeDailyVocabularyPlanCommand, DailyVocabularyPlanResponseDto> _initHandler;

    public GetOrInitializeTodayLearningSessionCommandHandler(
        ICommandHandler<InitializeDailyVocabularyPlanCommand, DailyVocabularyPlanResponseDto> initHandler)
    {
        _initHandler = initHandler;
    }

    public Task<DailyVocabularyPlanResponseDto> HandleAsync(
        GetOrInitializeTodayLearningSessionCommand command,
        CancellationToken ct = default)
    {
        return _initHandler.HandleAsync(
            new InitializeDailyVocabularyPlanCommand(
                command.UserId,
                new InitializeDailyPlanRequest(
                    ApiKey: null,
                    NewVocabularyTarget: command.Target ?? 20,
                    TimeZoneId: command.TimeZoneId ?? "Asia/Ho_Chi_Minh",
                    GenerateWithAiIfNeeded: false)),
            ct);
    }
}
