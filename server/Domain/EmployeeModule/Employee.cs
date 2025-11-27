using System.Diagnostics.CodeAnalysis;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.EmployeeModule;
public class Employee : TenantEntity<Employee>
{
    public Guid IdentityUserId { get; private set; }
    public User IdentityUser { get; private set; } = null!;

    public string Name { get; private set; }
    public DateOnly HireDate { get; private set; }
    public decimal Salary { get; private set; }

    [ExcludeFromCodeCoverage]
    private Employee() { }

    public Employee(Guid EmployeeId,Guid companyId,User identityUser, Guid identityUserId,
        string name, DateOnly hireDate, decimal salary)
    {
        CompanyId = companyId;
        IdentityUser = identityUser;
        IdentityUserId = identityUserId;

        Id = EmployeeId;
        Name = name;
        HireDate = hireDate;
        Salary = salary;
    }

    public override void Update(Employee updatedEntity)
    {
        Name = updatedEntity.Name;
        HireDate = updatedEntity.HireDate;
        Salary = updatedEntity.Salary;
    }

    public void UpdateOwnProfileName(string name)
    {
        Name = name;
    }
}