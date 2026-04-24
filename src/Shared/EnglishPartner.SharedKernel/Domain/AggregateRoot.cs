namespace EnglishPartner.SharedKernel.Domain;

/// <summary>
/// Base class for aggregate root entities.
/// Extends Entity with soft-delete, active status, and domain event support.
/// </summary>
public abstract class AggregateRoot : Entity
{
    public bool IsActive { get; protected set; } = true;
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    public Guid? DeletedBy { get; protected set; }

    private readonly List<DomainEvent> _domainEvents = [];

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(DomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public virtual void Activate() => IsActive = true;

    public virtual void Deactivate() => IsActive = false;

    public virtual void SoftDelete(Guid? deletedBy = null, DateTime? deletedAt = null)
    {
        IsDeleted = true;
        DeletedAt = deletedAt ?? DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }

    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        IsActive = true;
    }
}
