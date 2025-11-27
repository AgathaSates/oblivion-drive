namespace OblivionDrive.Domain.Shared;

public abstract class BaseEntity<T>
{
    public Guid Id { get; set; }
    public abstract void Update(T updatedEntity);
}

public abstract class TenantEntity<T> : BaseEntity<T>
{
    public Guid CompanyId { get; set; }
}