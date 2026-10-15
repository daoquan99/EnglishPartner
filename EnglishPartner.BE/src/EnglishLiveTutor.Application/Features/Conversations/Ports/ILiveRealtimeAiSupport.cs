using EnglishLiveTutor.Application.Features.Conversations.Contracts;

namespace EnglishLiveTutor.Application.Features.Conversations.Ports;

public interface ITurnTranslationService
{
    Task<string?> TranslateToVietnameseAsync(string apiKey, string englishText, CancellationToken ct = default);
}

public interface IGrammarCorrectionAnalyzer
{
    LanguageCorrectionDto? TryExtractCorrectionFromTeacherReply(
        string conversationId,
        string turnId,
        string? lastUserUtterance,
        string aiTeacherReply,
        string cefrLevel);

    Task<LanguageCorrectionDto?> AnalyzeUserUtteranceAsync(
        string apiKey,
        string conversationId,
        string turnId,
        string userTranscript,
        string cefrLevel,
        CancellationToken ct = default);
}
