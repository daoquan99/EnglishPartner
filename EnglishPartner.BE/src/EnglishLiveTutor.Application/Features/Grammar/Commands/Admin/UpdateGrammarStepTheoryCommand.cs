using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Validation;
using EnglishLiveTutor.Application.Features.Grammar.Rules;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin;

public sealed record UpdateGrammarStepTheoryCommand(
    string Code,
    JsonElement Theory
) : ICommand<Unit>;

public sealed class UpdateGrammarStepTheoryCommandHandler : ICommandHandler<UpdateGrammarStepTheoryCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGrammarStepTheoryCommandHandler(IAppDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> HandleAsync(UpdateGrammarStepTheoryCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
        {
            throw new NotFoundException($"Grammar step code '{command.Code}' not found.");
        }

        string normalizedCode = command.Code.Trim().ToUpperInvariant();

        var step = await _db.GrammarSteps
            .FirstOrDefaultAsync(s => s.Code == normalizedCode && !s.IsDeleted, ct);

        if (step == null)
        {
            throw new NotFoundException($"Grammar step '{normalizedCode}' was not found.");
        }

        string theoryJson = command.Theory.GetRawText();
        var validationIssues = GrammarTheoryValidator.Validate(theoryJson, step.IsLevelReview);

        var errorIssues = validationIssues.Where(i => string.Equals(i.Severity, "Error", StringComparison.OrdinalIgnoreCase)).ToList();
        if (errorIssues.Count > 0)
        {
            var failures = errorIssues.Select(i => new ValidationFailure(i.Path, i.Message));
            throw new ValidationException(failures);
        }

        step.EditTheoryByAdmin(theoryJson);
        await _unitOfWork.CommitChangesAsync(ct);

        return Unit.Value;
    }
}
