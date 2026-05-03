namespace EnglishPartner.SharedKernel.Domain;

/// <summary>
/// Base class for all entities. Provides identity and audit tracking.
/// Audit fields are auto-populated by AuditableEntityInterceptor.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public override bool Equals(object? obj)
        => obj is Entity entity && Id == entity.Id;

    public bool Equals(Entity? other)
        => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right)
        => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right)
        => !Equals(left, right);
}
