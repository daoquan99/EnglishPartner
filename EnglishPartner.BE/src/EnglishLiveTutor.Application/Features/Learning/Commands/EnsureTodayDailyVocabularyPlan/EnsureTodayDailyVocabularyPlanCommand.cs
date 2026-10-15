using EnglishLiveTutor.Application.Common.Cqrs;
using EnglishLiveTutor.Application.Features.Learning.Commands.InitializeDailyVocabularyPlan;
using EnglishLiveTutor.Application.Features.Learning.Contracts;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetDailyVocabularyPlan;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.EnsureTodayDailyVocabularyPlan;

/// <summary>
/// Compatibility use case for the legacy GET /api/learning/daily-plan endpoint.
/// Explicitly named as a Command because it auto-initializes today's plan when none exists yet
/// or when a non-default target/top-up is needed, while keeping GetDailyVocabularyPlanQuery strictly read-only.
/// </summary>
public sealed record EnsureTodayDailyVocabularyPlanCommand(
    string UserId,
    string? TimeZoneId,
    int? Target
) : ICommand<DailyVocabularyPlanResponseDto>;

public sealed class EnsureTodayDailyVocabularyPlanCommandHandler
    : ICommandHandler<EnsureTodayDailyVocabularyPlanCommand, DailyVocabularyPlanResponseDto>
{
    private readonly IQueryHandler<GetDailyVocabularyPlanQuery, DailyVocabularyPlanResponseDto?> _getQueryHandler;
    private readonly ICommandHandler<InitializeDailyVocabularyPlanCommand, DailyVocabularyPlanResponseDto> _initCommandHandler;

    public EnsureTodayDailyVocabularyPlanCommandHandler(
        IQueryHandler<GetDailyVocabularyPlanQuery, DailyVocabularyPlanResponseDto?> getQueryHandler,
        ICommandHandler<InitializeDailyVocabularyPlanCommand, DailyVocabularyPlanResponseDto> initCommandHandler)
    {
        _getQueryHandler = getQueryHandler;
        _initCommandHandler = initCommandHandler;
    }

    public async Task<DailyVocabularyPlanResponseDto> HandleAsync(
        EnsureTodayDailyVocabularyPlanCommand command,
        CancellationToken ct = default)
    {
        int effectiveTarget = command.Target ?? 20;
        string effectiveTz = command.TimeZoneId ?? "Asia/Ho_Chi_Minh";

        var existing = await _getQueryHandler.HandleAsync(
            new GetDailyVocabularyPlanQuery(command.UserId, effectiveTz, command.Target),
            ct);

        if (existing is not null &&
            existing.TotalAssignedCount > 0 &&
            (!command.Target.HasValue || existing.NewVocabularyTarget == effectiveTarget))
        {
            return existing;
        }

        return await _initCommandHandler.HandleAsync(
            new InitializeDailyVocabularyPlanCommand(
                command.UserId,
                new InitializeDailyPlanRequest(
                    ApiKey: null,
                    NewVocabularyTarget: effectiveTarget,
                    TimeZoneId: effectiveTz,
                    GenerateWithAiIfNeeded: false
                )),
            ct);
    }
}
