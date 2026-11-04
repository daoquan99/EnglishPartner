namespace EnglishLiveTutor.Application.Features.Identity.Contracts;

public sealed record UserNameInfo(
    Guid Id,
    string Username,
    string? FullName);
