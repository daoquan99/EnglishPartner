using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class FakeCurrentUser : ICurrentUser
{
    public string? UserId { get; set; } = "user-alpha";
}
