using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.EmployeeModule;

namespace OblivionDrive.Tests.Unit.EmployeeModule;

[TestClass]
[TestCategory("Employee - Employee Entity Unit Tests")]
public sealed class EmployeeTests
{
    private static User CreateIdentityUser()
    {
        return new User();
    }

    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        Guid employeeId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid identityUserId = Guid.NewGuid();
        DateOnly hireDate = new DateOnly(2024, 1, 1);
        decimal salary = 5000m;
        string employeeName = "João da Silva";

        User identityUser = CreateIdentityUser();

        // act
        Employee employee = new Employee(
            employeeId,
            companyId,
            identityUser,
            identityUserId,
            employeeName,
            hireDate,
            salary);

        // assert
        Assert.AreEqual(employeeId, employee.Id);
        Assert.AreEqual(companyId, employee.CompanyId);

        Assert.AreEqual(identityUserId, employee.IdentityUserId);
        Assert.AreSame(identityUser, employee.IdentityUser);

        Assert.AreEqual(employeeName, employee.Name);
        Assert.AreEqual(hireDate, employee.HireDate);
        Assert.AreEqual(salary, employee.Salary);
    }

    [TestMethod]
    public void Update_Should_Update_Name_HireDate_And_Salary_Only()
    {
        // arrange
        Guid originalEmployeeId = Guid.NewGuid();
        Guid originalCompanyId = Guid.NewGuid();
        Guid originalIdentityUserId = Guid.NewGuid();
        DateOnly originalHireDate = new DateOnly(2020, 1, 1);
        decimal originalSalary = 3000m;
        string originalName = "Nome Original";

        User originalIdentityUser = CreateIdentityUser();

        Employee employee = new Employee(
            originalEmployeeId,
            originalCompanyId,
            originalIdentityUser,
            originalIdentityUserId,
            originalName,
            originalHireDate,
            originalSalary);


        Guid updatedEmployeeId = Guid.NewGuid();
        Guid updatedCompanyId = Guid.NewGuid();
        Guid updatedIdentityUserId = Guid.NewGuid();
        DateOnly updatedHireDate = new DateOnly(2024, 5, 20);
        decimal updatedSalary = 8000m;
        string updatedName = "Nome Atualizado";

        User updatedIdentityUser = CreateIdentityUser();

        Employee updatedEmployee = new Employee(
            updatedEmployeeId,
            updatedCompanyId,
            updatedIdentityUser,
            updatedIdentityUserId,
            updatedName,
            updatedHireDate,
            updatedSalary);

        // act
        employee.Update(updatedEmployee);

        // assert 
        Assert.AreEqual(updatedName, employee.Name);
        Assert.AreEqual(updatedHireDate, employee.HireDate);
        Assert.AreEqual(updatedSalary, employee.Salary);

        Assert.AreEqual(originalEmployeeId, employee.Id);
        Assert.AreEqual(originalCompanyId, employee.CompanyId);
        Assert.AreEqual(originalIdentityUserId, employee.IdentityUserId);
        Assert.AreSame(originalIdentityUser, employee.IdentityUser);
    }

    [TestMethod]
    public void UpdateOwnProfileName_Should_Change_Only_Name()
    {
        // arrange
        Guid employeeId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid identityUserId = Guid.NewGuid();
        DateOnly hireDate = new DateOnly(2022, 10, 10);
        decimal salary = 4500m;
        string originalName = "Nome Original";

        User identityUser = CreateIdentityUser();

        Employee employee = new Employee(
            employeeId,
            companyId,
            identityUser,
            identityUserId,
            originalName,
            hireDate,
            salary);

        string newName = "Novo Nome";

        // act
        employee.UpdateOwnProfileName(newName);

        // assert 
        Assert.AreEqual(newName, employee.Name);

        Assert.AreEqual(employeeId, employee.Id);
        Assert.AreEqual(companyId, employee.CompanyId);
        Assert.AreEqual(identityUserId, employee.IdentityUserId);
        Assert.AreSame(identityUser, employee.IdentityUser);
        Assert.AreEqual(hireDate, employee.HireDate);
        Assert.AreEqual(salary, employee.Salary);
    }
}