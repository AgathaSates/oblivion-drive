using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.ClientModule;
public class ClientOrmRepository(OblivionDriveDbContext context) : BaseRepository<Client>(context), IRepositoryClient
{
    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await context.Clients
            .AnyAsync(client => client.Email == email);
    }

    public async Task<bool> ExistsByEmailAsync(string email, Guid clientIdToIgnore)
    {
        return await context.Clients
            .AnyAsync(client =>
                client.Email == email &&
                client.Id != clientIdToIgnore);
    }

    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber)
    {
        return await context.Clients
            .AnyAsync(client => client.PhoneNumber == phoneNumber);
    }

    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, Guid clientIdToIgnore)
    {
        return await context.Clients
            .AnyAsync(client =>
                client.PhoneNumber == phoneNumber &&
                client.Id != clientIdToIgnore);
    }

    public async Task<bool> ExistsByCpfAsync(string cpf)
    {
        return await context.Clients
            .AnyAsync(client => client.Cpf == cpf);
    }

    public async Task<bool> ExistsByCpfAsync(string cpf, Guid clientIdToIgnore)
    {
        return await context.Clients
            .AnyAsync(client =>
                client.Cpf == cpf &&
                client.Id != clientIdToIgnore);
    }

    public async Task<bool> ExistsByRgAsync(string rg)
    {
        return await context.Clients
            .AnyAsync(client => client.Rg == rg);
    }

    public async Task<bool> ExistsByRgAsync(string rg, Guid clientIdToIgnore)
    {
        return await context.Clients
            .AnyAsync(client =>
                client.Rg == rg &&
                client.Id != clientIdToIgnore);
    }

    public async Task<bool> ExistsByCnhAsync(string cnh)
    {
        return await context.Clients
            .AnyAsync(client => client.Cnh == cnh);
    }

    public async Task<bool> ExistsByCnhAsync(string cnh, Guid clientIdToIgnore)
    {
        return await context.Clients
            .AnyAsync(client =>
                client.Cnh == cnh &&
                client.Id != clientIdToIgnore);
    }

    public async Task<bool> ExistsByCnpjAsync(string cnpj)
    {
        return await context.Clients
            .AnyAsync(client => client.Cnpj == cnpj);
    }

    public async Task<bool> ExistsByCnpjAsync(string cnpj, Guid clientIdToIgnore)
    {
        return await context.Clients
            .AnyAsync(client =>
                client.Cnpj == cnpj &&
                client.Id != clientIdToIgnore);
    }
}