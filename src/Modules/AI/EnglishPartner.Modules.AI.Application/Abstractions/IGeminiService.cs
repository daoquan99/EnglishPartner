namespace EnglishPartner.Modules.AI.Application.Abstractions;

/// <summary>
/// Port for communicating with the Gemini LLM.
/// All AI interactions go through this interface.
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Evaluate a user's answer and return a quality score (0-5) compatible with SM-2.
    /// </summary>
    Task<EvaluationResult> EvaluateInputAsync(string itemContext, string userInput, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a conversation script from a topic name and level.
    /// </summary>
    Task<GeneratedConversation> GenerateConversationAsync(string topicName, string level, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a chat message with history and return the model response.
    /// </summary>
    Task<string> ChatAsync(string systemPrompt, IReadOnlyList<ChatTurn> history, string userMessage, CancellationToken cancellationToken = default);
}

public sealed record EvaluationResult(
    int QualityScore,
    bool IsCorrect,
    string SuggestedCorrection,
    string Explanation);

public sealed record GeneratedConversation(
    string Title,
    List<ConversationLine> Lines);

public sealed record ConversationLine(
    string Speaker,
    string Text,
    string Translation);

public sealed record ChatTurn(string Role, string Text);
