using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Domain;

/// <summary>
/// Domain errors for the AI module.
/// </summary>
public static class AiErrors
{
    public static class ChatSession
    {
        public static readonly Error NotFound = new("AI.ChatSession.NotFound", "Chat session not found.");
        public static readonly Error AccessDenied = new("AI.ChatSession.AccessDenied", "You do not have access to this chat session.");
    }

    public static class Evaluation
    {
        public static readonly Error ParseFailed = new("AI.Evaluation.ParseFailed", "AI returned an invalid response. Please try again.");
        public static readonly Error ServiceUnavailable = new("AI.Evaluation.ServiceUnavailable", "AI service is temporarily unavailable.");
    }

    public static class Generation
    {
        public static readonly Error ParseFailed = new("AI.Generation.ParseFailed", "AI failed to generate content. Please try again.");
    }
}
