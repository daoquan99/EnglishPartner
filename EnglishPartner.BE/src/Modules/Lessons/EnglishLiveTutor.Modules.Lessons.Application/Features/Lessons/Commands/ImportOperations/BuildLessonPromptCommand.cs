using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.ImportOperations;

public sealed record BuildLessonPromptCommand(
    string Topic,
    string CefrLevel,
    string? LessonTitle = null,
    List<string>? PreferredExerciseTypes = null,
    int TargetExerciseCount = 5,
    string? CustomInstructions = null
) : ICommand<LessonPromptResultDto>;

public sealed class BuildLessonPromptCommandHandler : ICommandHandler<BuildLessonPromptCommand, LessonPromptResultDto>
{
    private readonly ILessonPromptBuilder _promptBuilder;

    public BuildLessonPromptCommandHandler(ILessonPromptBuilder promptBuilder)
    {
        _promptBuilder = promptBuilder;
    }

    public Task<LessonPromptResultDto> HandleAsync(BuildLessonPromptCommand command, CancellationToken ct = default)
    {
        var input = new LessonPromptInput(
            Topic: command.Topic,
            CefrLevel: command.CefrLevel,
            LessonTitle: command.LessonTitle,
            PreferredExerciseTypes: command.PreferredExerciseTypes,
            TargetExerciseCount: command.TargetExerciseCount,
            CustomInstructions: command.CustomInstructions);

        return _promptBuilder.BuildPromptAsync(input, ct);
    }
}
