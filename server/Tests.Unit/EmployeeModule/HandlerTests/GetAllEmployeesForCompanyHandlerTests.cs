using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.EmployeeModule.DTOs;
using OblivionDrive.Application.EmployeeModule.Handlers;
using OblivionDrive.Application.EmployeeModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.EmployeeModule;

namespace OblivionDrive.Tests.Unit.EmployeeModule.HandlerTests;

[TestClass]
[TestCategory("Employee - GetAllEmployeesForCompanyHandler Unit Tests")]
public class GetAllEmployeesForCompanyHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryEmployee> _employeeRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetAllEmployeesForCompanyHandler>> _loggerMock = default!;
    private Mock<IValidator<GetAllEmployeesForCompanyQuery>> _validatorMock = null!;
    private GetAllEmployeesForCompanyHandler _handler = default!;

    [TestInitialize]
    public void Setup()
    {
        var userStore = new Mock<IUserStore<User>>();
        var identityOptions = Options.Create(new IdentityOptions());
        var passwordHasher = new Mock<IPasswordHasher<User>>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errorDescriber = new IdentityErrorDescriber();
        var serviceProvider = new Mock<IServiceProvider>();
        var loggerUserManager = new Mock<ILogger<UserManager<User>>>();

        _userManagerMock = new Mock<UserManager<User>>(
            userStore.Object,
            identityOptions,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            keyNormalizer.Object,
            errorDescriber,
            serviceProvider.Object,
            loggerUserManager.Object
        );

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(Guid.NewGuid());

        _employeeRepositoryMock = new Mock<IRepositoryEmployee>();

        _mapperMock = new Mock<IMapper>();

        _loggerMock = new Mock<ILogger<GetAllEmployeesForCompanyHandler>>();

        _validatorMock = new Mock<IValidator<GetAllEmployeesForCompanyQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetAllEmployeesForCompanyQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetAllEmployeesForCompanyHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _employeeRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object

        );
    }

    private static GetAllEmployeesForCompanyQuery CreateQuery(int? quantity = null)
    {
        return new GetAllEmployeesForCompanyQuery(
            Quantity: quantity
        );
    }

    private static User CreateCompanyUser(Guid userId)
    {
        return new User
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = userId
        };
    }

    private static Employee CreateEmployee(Guid employeeId, Guid companyId, string name)
    {
        Guid identityUserId = Guid.NewGuid();

        var identityUser = new User
        {
            Id = identityUserId,
            UserName = $"{name.Replace(" ", "").ToLower()}User",
            Email = $"{name.Replace(" ", "").ToLower()}@example.com",
            UserType = UserType.Employee,
            CompanyId = companyId
        };

        return new Employee(
            employeeId,
            companyId,
            identityUser,
            identityUserId,
            name,
            new DateOnly(2020, 1, 1),
            2000m
        );
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        var query = CreateQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<EmployeesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailEmployeeDTO>>(It.IsAny<IReadOnlyCollection<Employee>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        var query = CreateQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<EmployeesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);
        _employeeRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailEmployeeDTO>>(It.IsAny<IReadOnlyCollection<Employee>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Company()
    {
        // arrange
        var query = CreateQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        var currentUser = new User
        {
            Id = currentUserId,
            UserName = "employeeUser",
            Email = "employee@example.com",
            UserType = UserType.Employee
        };

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(currentUser);

        // act
        Result<EmployeesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);
        _employeeRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailEmployeeDTO>>(It.IsAny<IReadOnlyCollection<Employee>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InternalException_When_Repository_Throws()
    {
        // arrange
        var query = CreateQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _employeeRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("Erro ao listar funcionários"));

        // act
        Result<EmployeesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a listagem de funcionários da empresa")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mapperMock.Verify(m =>
            m.Map<List<DetailEmployeeDTO>>(It.IsAny<IReadOnlyCollection<Employee>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_All_Employees_When_Quantity_Is_Null()
    {
        // arrange
        var query = CreateQuery(quantity: null);

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var employees = new List<Employee>
        {
            CreateEmployee(Guid.NewGuid(), currentCompanyId, "Joao da Silva"),
            CreateEmployee(Guid.NewGuid(), currentCompanyId, "Maria Souza")
        };

        _employeeRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(employees);

        var employeeDtos = new List<DetailEmployeeDTO>
        {
            new(
                employees[0].Id,
                employees[0].Name,
                employees[0].HireDate,
                employees[0].Salary
            ),
            new(
                employees[1].Id,
                employees[1].Name,
                employees[1].HireDate,
                employees[1].Salary
            )
        };


        _mapperMock
            .Setup(m => m.Map<List<DetailEmployeeDTO>>(employees))
            .Returns(employeeDtos);

        // act
        Result<EmployeesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);

        var returnedEmployees = result.Value.Employees;
        Assert.AreEqual(employeeDtos.Count, returnedEmployees.Count);
        CollectionAssert.AreEqual(employeeDtos, returnedEmployees.ToList());

        _employeeRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);
        _employeeRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailEmployeeDTO>>(employees), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Limited_Employees_When_Quantity_Is_Specified()
    {
        // arrange
        const int quantity = 1;
        var query = CreateQuery(quantity);

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var employees = new List<Employee>
        {
            CreateEmployee(Guid.NewGuid(), currentCompanyId, "Joao da Silva")
        };

        _employeeRepositoryMock
            .Setup(r => r.GetAllAsync(quantity))
            .ReturnsAsync(employees);

        var employeeDtos = new List<DetailEmployeeDTO>
        {
            new(       
                employees[0].Id,
                employees[0].Name,
                employees[0].HireDate,
                employees[0].Salary
            )
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailEmployeeDTO>>(employees))
            .Returns(employeeDtos);

        // act
        Result<EmployeesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);

        var returnedEmployees = result.Value.Employees;
        Assert.AreEqual(1, returnedEmployees.Count);
        CollectionAssert.AreEqual(employeeDtos, returnedEmployees.ToList());

        _employeeRepositoryMock.Verify(r =>
            r.GetAllAsync(quantity), Times.Once);
        _employeeRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailEmployeeDTO>>(employees), Times.Once);
    }
}