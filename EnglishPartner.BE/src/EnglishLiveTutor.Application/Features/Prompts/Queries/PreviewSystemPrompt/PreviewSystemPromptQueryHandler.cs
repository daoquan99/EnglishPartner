using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.PreviewSystemPrompt;

public sealed class PreviewSystemPromptQueryHandler : IQueryHandler<PreviewSystemPromptQuery, string>
{
    private readonly ICefrPromptBuilder _promptBuilder;

    public PreviewSystemPromptQueryHandler(ICefrPromptBuilder promptBuilder)
    {
        _promptBuilder = promptBuilder;
    }

    public async Task<string> HandleAsync(PreviewSystemPromptQuery query, CancellationToken ct = default)
    {
        var req = query.Request;
        return await _promptBuilder.BuildSystemInstructionAsync(
            req.CefrLevel,
            req.ExtendedThinking,
            req.VoiceName,
            req.SpeechSpeed,
            req.ConversationMode,
            req.Topic,
            req.CustomSystemPrompt,
            previousMessages: null,
            ct: ct);
    }
}
