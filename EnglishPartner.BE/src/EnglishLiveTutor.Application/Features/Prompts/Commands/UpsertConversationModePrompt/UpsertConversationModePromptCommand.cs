using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Prompts.Repositories;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertConversationModePrompt;

public sealed record UpsertConversationModePromptCommand(
    UpsertConversationModePromptRequest Request
) : ICommand<ConversationModePromptEntity>;

public sealed class UpsertConversationModePromptCommandHandler : ICommandHandler<UpsertConversationModePromptCommand, ConversationModePromptEntity>
{
    private readonly IPromptConfigurationRepository _repo;

    public UpsertConversationModePromptCommandHandler(IPromptConfigurationRepository repo)
    {
        _repo = repo;
    }

    public async Task<ConversationModePromptEntity> HandleAsync(UpsertConversationModePromptCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var existing = await _repo.GetConversationModePromptAsync(req.ModeCode, ct);
        if (existing is null)
        {
            existing = new ConversationModePromptEntity(
                req.ModeCode,
                req.Category,
                req.DisplayName,
                req.DirectiveTemplate,
                req.DisplayOrder,
                req.IsDefault,
                req.IsActive);
            await _repo.AddConversationModePromptAsync(existing, ct);
        }
        else
        {
            existing.UpdateModePrompt(
                req.Category,
                req.DisplayName,
                req.DirectiveTemplate,
                req.DisplayOrder,
                req.IsDefault,
                req.IsActive);
        }

        await _repo.SaveChangesAsync(ct);
        return existing;
    }
}
