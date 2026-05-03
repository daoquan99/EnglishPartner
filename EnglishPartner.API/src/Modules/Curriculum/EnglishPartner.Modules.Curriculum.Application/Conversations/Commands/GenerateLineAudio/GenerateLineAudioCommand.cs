using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.GenerateLineAudio;

/// <summary>
/// Generates TTS audio for a ConversationLine and persists the resulting audioUrl.
/// If the line already has an audioUrl, it will be overwritten (re-generate).
/// </summary>
public sealed record GenerateLineAudioCommand(
    Guid ConversationId,
    Guid LineId,
    string? Voice = null) : ICommand<GenerateLineAudioResponse>;

public sealed record GenerateLineAudioResponse(string AudioUrl);
