using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.EmployeeModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.EmployeeModule;
public class EmployeeOrmRepository(OblivionDriveDbContext context) : BaseRepository<Employee>(context), IRepositoryEmployee
{
    public async Task<bool> ExistsByNameAsync(string employeeName, Guid employeeIdToIgnore)
    {
        return await context
            .Employees
            .AnyAsync(employee =>
                employee.Name == employeeName &&
                employee.Id != employeeIdToIgnore);
    }

    public async Task<bool> ExistsByNameAsync(string employeeName)
    {
        return await context
            .Employees
            .AnyAsync(employee => employee.Name == employeeName);
    }

    public Task<Employee> UpdateOwnProfileNameAsync(Employee employee, string newName)
    {
        employee!.UpdateOwnProfileName(newName);

        return Task.FromResult(employee);
    }
}