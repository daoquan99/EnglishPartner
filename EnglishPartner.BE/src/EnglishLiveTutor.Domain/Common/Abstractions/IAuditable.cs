namespace EnglishLiveTutor.Domain.Common.Abstractions;

public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; }
    string? CreatedBy { get; }
    DateTimeOffset? UpdatedAtUtc { get; }
    string? UpdatedBy { get; }
}
