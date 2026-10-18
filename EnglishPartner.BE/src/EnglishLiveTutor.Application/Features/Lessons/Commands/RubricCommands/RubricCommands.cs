using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Queries.RubricQueries;
using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.RubricCommands;

public sealed record CreateLessonRubricCommand(CreateLessonRubricRequest Request) : ICommand<LessonRubricDetailDto>;

public sealed record ActivateLessonRubricCommand(Guid RubricId) : ICommand;

public sealed record DeactivateLessonRubricCommand(Guid RubricId) : ICommand;

public sealed class RubricCommandHandlers :
    ICommandHandler<CreateLessonRubricCommand, LessonRubricDetailDto>,
    ICommandHandler<ActivateLessonRubricCommand>,
    ICommandHandler<DeactivateLessonRubricCommand>
{
    private readonly IAppDbContext _db;
    private readonly IQueryHandler<GetLessonRubricByIdQuery, LessonRubricDetailDto?> _getRubricByIdHandler;

    public RubricCommandHandlers(
        IAppDbContext db,
        IQueryHandler<GetLessonRubricByIdQuery, LessonRubricDetailDto?> getRubricByIdHandler)
    {
        _db = db;
        _getRubricByIdHandler = getRubricByIdHandler;
    }

    public async Task<LessonRubricDetailDto> HandleAsync(CreateLessonRubricCommand command, CancellationToken ct = default)
    {
        var req = command.Request;

        int? exType = null;
        if (!string.IsNullOrWhiteSpace(req.ExerciseType) && Enum.TryParse<ExerciseType>(req.ExerciseType, ignoreCase: true, out var parsedType))
            exType = (int)parsedType;

        string? applicableTypes = req.ApplicableExerciseTypes != null && req.ApplicableExerciseTypes.Count > 0
            ? string.Join(",", req.ApplicableExerciseTypes)
            : null;

        var rubric = new ScoringRubricEntity(
            code: req.Code,
            name: req.Name,
            description: req.Description,
            version: 1,
            assessmentType: "LessonExercise",
            isActive: true,
            assessmentScope: "LessonExercise",
            exerciseType: exType,
            subQuestionKind: req.SubQuestionKind,
            cefrLevel: string.IsNullOrWhiteSpace(req.CefrLevel) ? "All" : req.CefrLevel.Trim(),
            applicableExerciseTypes: applicableTypes);

        foreach (var c in req.Criteria)
        {
            rubric.AddCriterion(
                code: c.Code,
                name: c.Name,
                description: c.Description,
                weight: c.Weight,
                displayOrder: c.DisplayOrder,
                descriptorLevel0: c.DescriptorLevel0,
                descriptorLevel1: c.DescriptorLevel1,
                descriptorLevel2: c.DescriptorLevel2,
                descriptorLevel3: c.DescriptorLevel3,
                descriptorLevel4: c.DescriptorLevel4);
        }

        rubric.ValidateWeights();

        _db.ScoringRubrics.Add(rubric);
        await _db.SaveChangesAsync(ct);

        var dto = await _getRubricByIdHandler.HandleAsync(new GetLessonRubricByIdQuery(rubric.Id), ct);
        return dto!;
    }

    public async Task HandleAsync(ActivateLessonRubricCommand command, CancellationToken ct = default)
    {
        var rubric = await _db.ScoringRubrics.FirstOrDefaultAsync(r => r.Id == command.RubricId, ct)
            ?? throw new NotFoundException($"Rubric with ID '{command.RubricId}' not found.");

        rubric.Activate();
        await _db.SaveChangesAsync(ct);
    }

    public async Task HandleAsync(DeactivateLessonRubricCommand command, CancellationToken ct = default)
    {
        var rubric = await _db.ScoringRubrics.FirstOrDefaultAsync(r => r.Id == command.RubricId, ct)
            ?? throw new NotFoundException($"Rubric with ID '{command.RubricId}' not found.");

        rubric.Deactivate();
        await _db.SaveChangesAsync(ct);
    }
}
