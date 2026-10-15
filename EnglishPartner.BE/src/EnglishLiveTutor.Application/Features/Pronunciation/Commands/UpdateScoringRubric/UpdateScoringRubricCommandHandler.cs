using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.UpdateScoringRubric;

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
        string codeOrId = (command.CodeOrId ?? string.Empty).Trim();

        if (req.Criteria is null || req.Criteria.Count == 0)
            throw new InvalidOperationException("Rubric phải có ít nhất 1 tiêu chí chấm điểm.");

        ScoringRubricEntity? existing = null;
        if (Guid.TryParse(codeOrId, out var parsedId))
        {
            existing = await _rubricRepo.GetByIdWithCriteriaAsync(parsedId, ct);
        }

        if (existing is null && !string.IsNullOrWhiteSpace(codeOrId))
        {
            existing = await _rubricRepo.GetByCodeWithCriteriaAsync(codeOrId, ct);
        }

        if (existing is null)
        {
            string effectiveCode = string.IsNullOrWhiteSpace(codeOrId) || Guid.TryParse(codeOrId, out _)
                ? $"RUBRIC_{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
                : codeOrId.ToUpperInvariant();

            var created = new ScoringRubricEntity(
                code: effectiveCode,
                name: string.IsNullOrWhiteSpace(req.Name) ? effectiveCode : req.Name.Trim(),
                description: req.Description ?? string.Empty,
                version: 1,
                assessmentType: "Pronunciation",
                isActive: true);

            int order = 1;
            foreach (var c in req.Criteria)
            {
                created.AddCriterion(c.Code, c.Name, c.Description, c.Weight, order++);
            }
            created.ValidateWeights();

            await _rubricRepo.AddAsync(created, ct);
            await _rubricRepo.SaveChangesAsync(ct);
            return created;
        }

        existing.UpdateRubric(
            req.Name,
            req.Description,
            req.Criteria.Select(c => (c.Code, c.Name, c.Description, c.Weight)));

        await _rubricRepo.SaveChangesAsync(ct);
        return existing;
    }
}
