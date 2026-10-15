using EnglishLiveTutor.Application.Features.Conversations.Contracts;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Entities;

namespace EnglishLiveTutor.Application.Features.Conversations.Mapping;

public static class ConversationMapping
{
    public static ChatMessageDto ToDto(this ChatMessageEntity m) =>
        new(
            Id: m.Id,
            ConversationId: m.ConversationId,
            TurnId: m.TurnId,
            Speaker: m.Speaker,
            TranscriptLine1: m.TranscriptLine1,
            TranslationLine2: m.TranslationLine2,
            IsFinal: m.IsFinal,
            SequenceOrder: m.SequenceOrder,
            CreatedAtUtc: m.CreatedAtUtc,
            CreatedBy: m.CreatedBy,
            UpdatedAtUtc: m.UpdatedAtUtc,
            UpdatedBy: m.UpdatedBy,
            IsDeleted: m.IsDeleted,
            DeletedAtUtc: m.DeletedAtUtc,
            DeletedBy: m.DeletedBy,
            CreatedAt: m.CreatedAt,
            UpdatedAt: m.UpdatedAt
        );

    public static LanguageCorrectionItemDto ToDto(this LanguageCorrectionEntity c) =>
        new(
            Id: c.Id,
            ConversationId: c.ConversationId,
            TurnId: c.TurnId,
            OriginalText: c.OriginalText,
            CorrectedText: c.CorrectedText,
            ErrorCategory: c.ErrorCategory,
            ExplanationVi: c.ExplanationVi,
            CefrTip: c.CefrTip,
            CreatedAtUtc: c.CreatedAtUtc,
            CreatedBy: c.CreatedBy,
            UpdatedAtUtc: c.UpdatedAtUtc,
            UpdatedBy: c.UpdatedBy,
            IsDeleted: c.IsDeleted,
            DeletedAtUtc: c.DeletedAtUtc,
            DeletedBy: c.DeletedBy,
            CreatedAt: c.CreatedAt,
            UpdatedAt: c.UpdatedAt
        );

    public static ConversationDetailDto ToDetailDto(this ConversationEntity entity) =>
        new(
            Id: entity.Id,
            Title: entity.Title,
            CefrLevel: entity.CefrLevel,
            ExtendedThinking: entity.ExtendedThinking,
            VoiceName: entity.VoiceName,
            SpeechSpeed: entity.SpeechSpeed,
            ConversationMode: entity.ConversationMode,
            Topic: entity.Topic,
            CustomSystemPrompt: entity.CustomSystemPrompt,
            Messages: entity.Messages
                .Where(m => !m.IsDeleted && (m.TranscriptLine1 != "" || m.TranslationLine2 != ""))
                .OrderBy(m => m.SequenceOrder)
                .Select(ToDto)
                .ToList(),
            Corrections: entity.Corrections
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(ToDto)
                .ToList(),
            CreatedAtUtc: entity.CreatedAtUtc,
            CreatedBy: entity.CreatedBy,
            UpdatedAtUtc: entity.UpdatedAtUtc,
            UpdatedBy: entity.UpdatedBy,
            IsDeleted: entity.IsDeleted,
            DeletedAtUtc: entity.DeletedAtUtc,
            DeletedBy: entity.DeletedBy,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt
        );
}
