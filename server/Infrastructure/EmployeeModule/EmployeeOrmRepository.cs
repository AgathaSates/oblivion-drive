using OblivionDrive.Domain.EmployeeModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.EmployeeModule;
public class EmployeeOrmRepository(OblivionDriveDbContext context) : BaseRepository<Employee>(context), IRepositoryEmployee
{
    public Task<Employee> UpdateOwnProfileNameAsync(Employee employee, string newName)
    {
        employee!.UpdateOwnProfileName(newName);

        return Task.FromResult(employee);
    }
}