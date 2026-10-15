using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Prompts;

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
