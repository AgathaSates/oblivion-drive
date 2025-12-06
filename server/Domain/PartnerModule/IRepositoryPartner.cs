using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.PartnerModule;
public interface IRepositoryPartner : IRepository<Partner>
{
    Task<bool> ExistsByNameAsync(string partnerName);
    Task<bool> ExistsByNameAsync(string partnerName, Guid partnerIdToIgnore);
}
