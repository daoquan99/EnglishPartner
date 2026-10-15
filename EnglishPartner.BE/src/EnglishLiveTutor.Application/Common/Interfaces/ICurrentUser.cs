namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface ICurrentUser
{
    string? UserId { get; }
    string UserIdOrSystem => string.IsNullOrWhiteSpace(UserId) ? "system" : UserId!;
}
