namespace EnglishLiveTutor.Domain.Common.Abstractions;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAtUtc { get; }
    string? DeletedBy { get; }

    void SoftDelete(DateTimeOffset deletedAtUtc, string? deletedBy = null);
    void Restore();
}
