using System.Text.RegularExpressions;
using EnglishLiveTutor.Application.Common.Exceptions;

namespace EnglishLiveTutor.Application.Features.Conversations.Validation;

public static class ConversationIdValidator
{
    private static readonly Regex ValidPattern = new(@"^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    public static string NormalizeAndValidate(string? conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        var trimmed = conversationId.Trim();
        if (trimmed.Length > 64 || !ValidPattern.IsMatch(trimmed))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        return trimmed;
    }
}
