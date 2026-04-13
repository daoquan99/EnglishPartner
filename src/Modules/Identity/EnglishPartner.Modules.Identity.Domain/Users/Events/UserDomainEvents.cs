using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Identity.Domain.Users.Events;

/// <summary>
/// Raised when a new user registers.
/// </summary>
public sealed record UserRegisteredDomainEvent(Guid UserId, string Email, string DisplayName) : DomainEvent;

/// <summary>
/// Raised when user completes the onboarding flow.
/// </summary>
public sealed record OnboardingCompletedDomainEvent(Guid UserId) : DomainEvent;

/// <summary>
/// Raised when user's streak is updated via daily check-in.
/// </summary>
public sealed record StreakUpdatedDomainEvent(Guid UserId, int CurrentStreak, int LongestStreak) : DomainEvent;
