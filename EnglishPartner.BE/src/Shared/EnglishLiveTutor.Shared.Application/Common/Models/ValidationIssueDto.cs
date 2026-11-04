namespace EnglishLiveTutor.Application.Common.Models;

public sealed record ValidationIssueDto(
    string Path,
    string Severity, // Error | Warning
    string Message
);
