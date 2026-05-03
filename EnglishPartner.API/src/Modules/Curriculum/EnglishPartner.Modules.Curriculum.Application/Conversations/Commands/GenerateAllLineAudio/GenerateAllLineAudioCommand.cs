using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.GenerateAllLineAudio;

/// <summary>
/// Generates TTS audio for ALL lines in a conversation that are missing audioUrl.
/// Returns count of successfully generated lines.
/// </summary>
public sealed record GenerateAllLineAudioCommand(
    Guid ConversationId,
    string? Voice = null) : ICommand<GenerateAllLineAudioResponse>;

public sealed record GenerateAllLineAudioResponse(
    int TotalLines,
    int GeneratedCount,
    int SkippedCount,
    List<GeneratedLineInfo> GeneratedLines);

public sealed record GeneratedLineInfo(Guid LineId, string AudioUrl);
