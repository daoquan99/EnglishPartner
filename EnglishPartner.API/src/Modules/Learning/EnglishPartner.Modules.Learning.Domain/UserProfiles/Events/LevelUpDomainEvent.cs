using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Learning.Domain.UserProfiles.Events;

/// <summary>
/// Raised when a user's CEFR level increases after earning XP.
/// Consumed by: Learning.Application → LevelUpIntegrationEvent → Notifications (level-up push).
/// </summary>
public sealed record LevelUpDomainEvent(
    Guid ProfileId,
    Guid UserId,
    string PreviousLevel,
    string NewLevel,
    int TotalXp) : DomainEvent;
