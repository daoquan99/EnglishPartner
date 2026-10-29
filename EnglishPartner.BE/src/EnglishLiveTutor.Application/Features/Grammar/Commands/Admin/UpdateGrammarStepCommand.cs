using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.Grammar.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin;

public sealed record UpdateGrammarStepCommand(
    string Code,
    string TitleVi,
    string TitleEn,
    string SummaryVi,
    bool IsPublished
) : ICommand<GrammarStepAdminDto>;

public sealed class UpdateGrammarStepCommandHandler : ICommandHandler<UpdateGrammarStepCommand, GrammarStepAdminDto>
{
    private readonly IAppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGrammarStepCommandHandler(IAppDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task<GrammarStepAdminDto> HandleAsync(UpdateGrammarStepCommand command, CancellationToken ct = default)
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

        step.UpdateMetadata(command.TitleVi, command.TitleEn, command.SummaryVi, command.IsPublished);
        await _unitOfWork.CommitChangesAsync(ct);

        var counts = await _db.GrammarExercises
            .AsNoTracking()
            .Where(e => e.StepId == step.Id && !e.IsDeleted)
            .GroupBy(e => 1)
            .Select(g => new
            {
                CoreCount = g.Count(e => e.Source == GrammarExerciseSource.Official && e.IsCore),
                OfficialCount = g.Count(e => e.Source == GrammarExerciseSource.Official),
                PendingCount = g.Count(e => e.Moderation == GrammarModerationStatus.Pending)
            })
            .FirstOrDefaultAsync(ct);

        return new GrammarStepAdminDto(
            Id: step.Id,
            Code: step.Code,
            CefrLevel: step.CefrLevel,
            Order: step.Order,
            TitleVi: step.TitleVi,
            TitleEn: step.TitleEn,
            SummaryVi: step.SummaryVi,
            IsLevelReview: step.IsLevelReview,
            IsPublished: step.IsPublished,
            TheoryEditedByAdmin: step.TheoryEditedByAdmin,
            TheoryVersion: step.TheoryVersion,
            CoreExerciseCount: counts?.CoreCount ?? 0,
            OfficialExerciseCount: counts?.OfficialCount ?? 0,
            PendingExerciseCount: counts?.PendingCount ?? 0,
            CreatedAtUtc: step.CreatedAtUtc,
            UpdatedAtUtc: step.UpdatedAtUtc
        );
    }
}
