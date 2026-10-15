using EnglishLiveTutor.Application.Features.Prompts.Contracts;
using EnglishLiveTutor.Domain.Prompts.CefrLevelPrompts;
using EnglishLiveTutor.Domain.Prompts.ConversationModePrompts;
using EnglishLiveTutor.Domain.Prompts.SpeechSpeeds;

namespace EnglishLiveTutor.Application.Features.Prompts.Mapping;

public static class PromptMapping
{
    public static CefrLevelPromptDto ToDto(this CefrLevelPromptEntity e) => new(
        e.Id,
        e.CefrLevel,
        e.DisplayName,
        e.LevelGuidance,
        e.DisplayOrder,
        e.IsActive,
        e.CreatedAtUtc,
        e.CreatedBy,
        e.UpdatedAtUtc,
        e.UpdatedBy,
        e.IsDeleted,
        e.DeletedAtUtc,
        e.DeletedBy
    );

    public static ConversationModePromptDto ToDto(this ConversationModePromptEntity e) => new(
        e.Id,
        e.ModeCode,
        e.Category,
        e.DisplayName,
        e.DirectiveTemplate,
        e.DisplayOrder,
        e.IsDefault,
        e.IsActive,
        e.CreatedAtUtc,
        e.CreatedBy,
        e.UpdatedAtUtc,
        e.UpdatedBy,
        e.IsDeleted,
        e.DeletedAtUtc,
        e.DeletedBy
    );

    public static SpeechSpeedOptionDto ToDto(this SpeechSpeedEntity e) => new(
        e.Id,
        e.Code,
        e.SpeedValue,
        e.Label,
        e.DescriptionVi,
        e.RecommendedCefrRange,
        e.IsDefault,
        e.IsActive,
        e.DisplayOrder
    );
}
