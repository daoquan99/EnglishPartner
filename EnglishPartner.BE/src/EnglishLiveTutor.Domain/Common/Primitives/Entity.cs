using EnglishLiveTutor.Domain.Common.Abstractions;

namespace EnglishLiveTutor.Domain.Common.Primitives;

public abstract class Entity<TId> : IAuditable, IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    protected Entity()
    {
    }

    protected Entity(TId id)
    {
        Id = id;
    }

    public void SetCreationAudit(DateTimeOffset createdAtUtc, string? createdBy = null)
    {
        CreatedAtUtc = createdAtUtc;
        if (!string.IsNullOrWhiteSpace(createdBy))
        {
            CreatedBy = createdBy;
        }
    }

    public void SetModificationAudit(DateTimeOffset updatedAtUtc, string? updatedBy = null)
    {
        UpdatedAtUtc = updatedAtUtc;
        if (!string.IsNullOrWhiteSpace(updatedBy))
        {
            UpdatedBy = updatedBy;
        }
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (EqualityComparer<TId>.Default.Equals(Id, default!) || EqualityComparer<TId>.Default.Equals(other.Id, default!))
            return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() =>
        EqualityComparer<TId>.Default.Equals(Id, default!) ? base.GetHashCode() : EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !Equals(left, right);
}
