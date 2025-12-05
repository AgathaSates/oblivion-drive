using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.ClientModule;

public interface IRepositoryClient : IRepository<Client>
{
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email, Guid clientIdToIgnore);

    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber);
    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, Guid clientIdToIgnore);

    Task<bool> ExistsByCpfAsync(string cpf);
    Task<bool> ExistsByCpfAsync(string cpf, Guid clientIdToIgnore);

    Task<bool> ExistsByRgAsync(string rg);
    Task<bool> ExistsByRgAsync(string rg, Guid clientIdToIgnore);

    Task<bool> ExistsByCnhAsync(string cnh);
    Task<bool> ExistsByCnhAsync(string cnh, Guid clientIdToIgnore);

    Task<bool> ExistsByCnpjAsync(string cnpj);
    Task<bool> ExistsByCnpjAsync(string cnpj, Guid clientIdToIgnore);
}