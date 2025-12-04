using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.ServicesModule;
public class ServicesOrmRepository(OblivionDriveDbContext context) : BaseRepository<Service>(context), IRepositoryServices
{
    public async Task<bool> ExistsByNameAsync(string serviceName, Guid serviceIdToIgnore) 
    {
        return await context.Services.AnyAsync(s => s.Name == serviceName && s.Id != serviceIdToIgnore);
    }

    public async Task<bool> ExistsByNameAsync(string serviceName)
    {
        return await context.Services.AnyAsync(s => s.Name == serviceName);
    }
}