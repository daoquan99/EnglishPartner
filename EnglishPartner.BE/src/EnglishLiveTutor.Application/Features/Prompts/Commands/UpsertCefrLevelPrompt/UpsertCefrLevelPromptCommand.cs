using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Prompts.Repositories;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertCefrLevelPrompt;

public sealed record UpsertCefrLevelPromptCommand(
    string CefrLevel,
    UpsertCefrLevelPromptRequest Request
) : ICommand<CefrLevelPromptEntity>
{
    public UpsertCefrLevelPromptCommand(UpsertCefrLevelPromptRequest request)
        : this(!string.IsNullOrWhiteSpace(request.DisplayName) && request.DisplayName.Length <= 4
            ? request.DisplayName
            : "B1", request)
    {
    }
}

public sealed class UpsertCefrLevelPromptCommandHandler : ICommandHandler<UpsertCefrLevelPromptCommand, CefrLevelPromptEntity>
{
    private readonly IPromptConfigurationRepository _repo;

    public UpsertCefrLevelPromptCommandHandler(IPromptConfigurationRepository repo)
    {
        _repo = repo;
    }

    public async Task<CefrLevelPromptEntity> HandleAsync(UpsertCefrLevelPromptCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var existing = await _repo.GetCefrLevelPromptAsync(command.CefrLevel, ct);
        if (existing is null)
        {
            existing = new CefrLevelPromptEntity(
                command.CefrLevel,
                req.DisplayName,
                req.LevelGuidance,
                req.DisplayOrder,
                req.IsActive);
            await _repo.AddCefrLevelPromptAsync(existing, ct);
        }
        else
        {
            existing.UpdateGuidance(req.DisplayName, req.LevelGuidance, req.DisplayOrder, req.IsActive);
        }

        await _repo.SaveChangesAsync(ct);
        return existing;
    }
}
