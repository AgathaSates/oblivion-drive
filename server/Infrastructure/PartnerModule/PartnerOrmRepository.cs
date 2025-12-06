using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.PartnerModule;
public class PartnerOrmRepository(OblivionDriveDbContext context) : BaseRepository<Partner>(context), IRepositoryPartner
{
    public async Task<bool> ExistsByNameAsync(string partnerName)
    {
        return await context.Partners
            .AnyAsync(p => p.Name == partnerName);
    }

    public async Task<bool> ExistsByNameAsync(string partnerName, Guid partnerIdToIgnore)
    {
        return await context.Partners
            .AnyAsync(p => p.Name == partnerName && p.Id != partnerIdToIgnore);
    }
}