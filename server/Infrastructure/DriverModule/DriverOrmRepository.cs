using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.DriverModule;
public class DriverOrmRepository(OblivionDriveDbContext context) : BaseRepository<Driver>(context), IRepositoryDriver
{
    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await context.Drivers
            .AnyAsync(driver => driver.Email == email);
    }

    public async Task<bool> ExistsByEmailAsync(string email, Guid driverIdToIgnore)
    {
        return await context.Drivers
            .AnyAsync(driver =>
                driver.Email == email &&
                driver.Id != driverIdToIgnore);
    }

    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber)
    {
        return await context.Drivers
            .AnyAsync(driver => driver.PhoneNumber == phoneNumber);
    }

    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, Guid driverIdToIgnore)
    {
        return await context.Drivers
            .AnyAsync(driver =>
                driver.PhoneNumber == phoneNumber &&
                driver.Id != driverIdToIgnore);
    }

    public async Task<bool> ExistsByCpfAsync(string cpf)
    {
        return await context.Drivers
            .AnyAsync(driver => driver.Cpf == cpf);
    }

    public async Task<bool> ExistsByCpfAsync(string cpf, Guid driverIdToIgnore)
    {
        return await context.Drivers
            .AnyAsync(driver =>
                driver.Cpf == cpf &&
                driver.Id != driverIdToIgnore);
    }

    public async Task<bool> ExistsByCnhAsync(string cnh)
    {
        return await context.Drivers
            .AnyAsync(driver => driver.Cnh == cnh);
    }

    public async Task<bool> ExistsByCnhAsync(string cnh, Guid driverIdToIgnore)
    {
        return await context.Drivers
            .AnyAsync(driver =>
                driver.Cnh == cnh &&
                driver.Id != driverIdToIgnore);
    }
}