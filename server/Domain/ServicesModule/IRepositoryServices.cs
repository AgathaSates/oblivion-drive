using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.ServicesModule;

public interface IRepositoryServices : IRepository<Service>
{
    Task<bool> ExistsByNameAsync(string serviceName);
    Task<bool> ExistsByNameAsync(string serviceName, Guid serviceIdToIgnore);
}