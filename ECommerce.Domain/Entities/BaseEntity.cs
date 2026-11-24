namespace ECommerce.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public bool IsDeleted { get; private set; }

    public void MarkAsDeleted()
    {
        // UpdatedAt is stamped by AuditInterceptor when the change is saved.
        IsDeleted = true;
    }
}
