namespace OblivionDrive.Domain.Shared;

public abstract class BaseEntity<T>
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public abstract void Update(T updatedEntity);
}