using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.AI.Application.ContentGeneration.Commands.GenerateConversation;

/// <summary>
/// Asks Gemini to generate a full conversation script for a given topic and level.
/// Used by admins to auto-populate Curriculum content.
/// </summary>
public sealed record GenerateConversationCommand(
    string TopicName,
    string Level) : ICommand<GenerateConversationResponse>;

public sealed record GenerateConversationResponse(
    string Title,
    List<GeneratedLine> Lines);

public sealed record GeneratedLine(
    string Speaker,
    string Text,
    string Translation);
