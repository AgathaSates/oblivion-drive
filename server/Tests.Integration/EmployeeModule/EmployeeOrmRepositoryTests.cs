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

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_True_When_Employee_With_Same_Name_Exists()
    {
        // arrange
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

        string employeeName = "Funcionário Teste";

        var employee = new Employee(
            EmployeeId: Guid.NewGuid(),
            companyId: companyId,
            identityUser: identityUser,
            identityUserId: identityUserId,
            name: employeeName,
            hireDate: new DateOnly(2020, 1, 1),
            salary: 2000m
        );

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await employeeRepository.ExistsByNameAsync(employeeName);

        // assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_False_When_Employee_With_Name_Does_Not_Exist()
    {
        // arrange
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

        var existingEmployee = new Employee(
            EmployeeId: Guid.NewGuid(),
            companyId: companyId,
            identityUser: identityUser,
            identityUserId: identityUserId,
            name: "Funcionário Existente",
            hireDate: new DateOnly(2020, 1, 1),
            salary: 2000m
        );

        dbContext.Employees.Add(existingEmployee);
        await dbContext.SaveChangesAsync();

        string searchedName = "Outro Funcionário";

        // act
        bool exists = await employeeRepository.ExistsByNameAsync(searchedName);

        // assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_False_When_Name_Is_Empty_Or_Whitespace()
    {
        // arrange
        var employeeRepository = _employeeRepository ?? throw new InvalidOperationException("Employee repository not initialized.");

        // act
        bool existsForEmpty = await employeeRepository.ExistsByNameAsync(string.Empty);
        bool existsForWhitespace = await employeeRepository.ExistsByNameAsync("   ");

        // assert
        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_WithIgnoreId_Should_Return_False_When_Only_Employee_With_Name_Is_Self()
    {
        // arrange
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

        string employeeName = "Funcionário Teste";

        var employee = new Employee(
            EmployeeId: Guid.NewGuid(),
            companyId: companyId,
            identityUser: identityUser,
            identityUserId: identityUserId,
            name: employeeName,
            hireDate: new DateOnly(2020, 1, 1),
            salary: 2000m
        );

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await employeeRepository.ExistsByNameAsync(employeeName, employee.Id);

        // assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio funcionário como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByNameAsync_WithIgnoreId_Should_Return_True_When_Other_Employee_With_Same_Name_Exists()
    {
        // arrange
        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        var employeeRepository = _employeeRepository ?? throw new InvalidOperationException("Employee repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Guid identityUserId1 = Guid.NewGuid();
        var identityUser1 = new User
        {
            Id = identityUserId1,
            UserName = "employeeUser1",
            Email = "employee1@example.com",
            UserType = UserType.Employee,
            CompanyId = companyId
        };

        dbContext.Users.Add(identityUser1);

        string employeeName = "Funcionário Teste";

        var employee1 = new Employee(
            EmployeeId: Guid.NewGuid(),
            companyId: companyId,
            identityUser: identityUser1,
            identityUserId: identityUserId1,
            name: employeeName,
            hireDate: new DateOnly(2020, 1, 1),
            salary: 2000m
        );

        dbContext.Employees.Add(employee1);

        Guid identityUserId2 = Guid.NewGuid();
        var identityUser2 = new User
        {
            Id = identityUserId2,
            UserName = "employeeUser2",
            Email = "employee2@example.com",
            UserType = UserType.Employee,
            CompanyId = companyId
        };

        dbContext.Users.Add(identityUser2);

        var employee2 = new Employee(
            EmployeeId: Guid.NewGuid(),
            companyId: companyId,
            identityUser: identityUser2,
            identityUserId: identityUserId2,
            name: employeeName,
            hireDate: new DateOnly(2020, 1, 2),
            salary: 2500m
        );

        dbContext.Employees.Add(employee2);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await employeeRepository.ExistsByNameAsync(employeeName, employee1.Id);

        // assert
        Assert.IsTrue(exists, "Deveria detectar outro funcionário com o mesmo nome.");
    }

}