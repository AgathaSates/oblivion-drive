namespace OblivionDrive.Domain.Shared;

public interface IUnitOfWork
{
    Task CommitAsync();
    Task RollbackAsync();
}