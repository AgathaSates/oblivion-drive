using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.EmployeeModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.EmployeeModule;

[TestClass]
[TestCategory("EmployeeOrmRepository Infrastructure - Integration Tests")]
public class EmployeeOrmRepositoryTests : TestFixture
{
    [TestMethod]
    public async Task UpdateOwnProfileNameAsync_Should_Update_Name_And_Persist_In_Database()
    {
        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        var employeeRepository = _employeeRepository ?? throw new InvalidOperationException("Employee repository not initialized.");

        Guid companyId = Guid.NewGuid();
        Guid identityUserId = Guid.NewGuid();

        var identityUser = new User
        {
            Id = identityUserId,
            UserName = "employeeUser",
            Email = "employee@example.com",
            UserType = UserType.Employee,
            CompanyId = companyId
        };

        dbContext.Users.Add(identityUser);

        var employee = new Employee(
            EmployeeId: Guid.NewGuid(),
            companyId: companyId,
            identityUser: identityUser,
            identityUserId: identityUserId,
            name: "Nome Original",
            hireDate: new DateOnly(2020, 1, 1),
            salary: 2000m
        );

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();

        string newName = "Nome Atualizado";

        // act
        Employee result = await employeeRepository.UpdateOwnProfileNameAsync(employee, newName);
        await dbContext.SaveChangesAsync();

        // assert
        Assert.AreEqual(newName, result.Name);

        Employee? fromDb = await dbContext.Employees.SingleOrDefaultAsync(e => e.Id == employee.Id);
        Assert.IsNotNull(fromDb);
        Assert.AreEqual(newName, fromDb!.Name);
    }
}