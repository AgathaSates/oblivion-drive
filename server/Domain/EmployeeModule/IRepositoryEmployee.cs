using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.EmployeeModule;
public interface IRepositoryEmployee : IRepository<Employee> 
{
    Task<Employee> UpdateOwnProfileNameAsync(Employee employee, string newName);
    Task<bool> ExistsByNameAsync(string employeeName, Guid employeeIdToIgnore);
    Task<bool> ExistsByNameAsync(string employeeName);
}