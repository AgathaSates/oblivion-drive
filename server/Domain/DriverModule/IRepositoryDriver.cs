using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.DriverModule;
public interface IRepositoryDriver : IRepository<Driver>
{
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email, Guid driverIdToIgnore);

    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber);
    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, Guid driverIdToIgnore);

    Task<bool> ExistsByCpfAsync(string cpf);
    Task<bool> ExistsByCpfAsync(string cpf, Guid driverIdToIgnore);

    Task<bool> ExistsByCnhAsync(string cnh);
    Task<bool> ExistsByCnhAsync(string cnh, Guid driverIdToIgnore);
}