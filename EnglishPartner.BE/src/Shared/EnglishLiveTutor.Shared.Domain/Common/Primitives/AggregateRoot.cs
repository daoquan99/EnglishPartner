using EnglishLiveTutor.Domain.Common.Abstractions;

namespace EnglishLiveTutor.Domain.Common.Primitives;

public abstract class AggregateRoot<TId> : Entity<TId>, ISoftDeletable
    where TId : notnull
{
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAtUtc { get; protected set; }
    public string? DeletedBy { get; protected set; }

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }

    public virtual void SoftDelete(DateTimeOffset deletedAtUtc, string? deletedBy = null)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        DeletedBy = string.IsNullOrWhiteSpace(deletedBy) ? null : deletedBy.Trim();
    }

    public virtual void Restore()
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }
}
