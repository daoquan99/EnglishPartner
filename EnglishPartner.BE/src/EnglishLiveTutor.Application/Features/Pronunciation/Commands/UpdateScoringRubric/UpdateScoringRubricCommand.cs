using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.UpdateScoringRubric;

public sealed record UpdateScoringRubricCommand(
    Guid RubricId,
    UpdateScoringRubricRequest Request
) : ICommand<ScoringRubricEntity>;

public sealed class UpdateScoringRubricCommandHandler : ICommandHandler<UpdateScoringRubricCommand, ScoringRubricEntity>
{
    private readonly IScoringRubricRepository _rubricRepo;

    public UpdateScoringRubricCommandHandler(IScoringRubricRepository rubricRepo)
    {
        _rubricRepo = rubricRepo;
    }

    public async Task<ScoringRubricEntity> HandleAsync(UpdateScoringRubricCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var rubricId = command.RubricId;

        if (req.Criteria is null || req.Criteria.Count == 0)
            throw new InvalidOperationException("Rubric phải có ít nhất 1 tiêu chí chấm điểm.");

        var existing = await _rubricRepo.GetByIdWithCriteriaAsync(rubricId, ct);
        if (existing is null)
            throw new InvalidOperationException("Không tìm thấy cấu hình Rubric.");

        existing.UpdateRubric(
            req.Name,
            req.Description,
            req.Criteria.Select(c => (c.Code, c.Name, c.Description, c.Weight)));

        await _rubricRepo.SaveChangesAsync(ct);
        return existing;
    }
}
