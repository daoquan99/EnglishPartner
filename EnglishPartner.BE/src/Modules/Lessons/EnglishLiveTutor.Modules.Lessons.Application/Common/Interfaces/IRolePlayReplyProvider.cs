namespace EnglishLiveTutor.Application.Common.Interfaces;

public sealed record RolePlayHistoryItem(string Role, string Content);

public interface IRolePlayReplyProvider
{
    Task<string> GenerateReplyAsync(
        string scenario,
        string aiRole,
        IReadOnlyList<RolePlayHistoryItem> conversationHistory,
        int currentTurn,
        int maxTurns,
        CancellationToken ct = default);
}
