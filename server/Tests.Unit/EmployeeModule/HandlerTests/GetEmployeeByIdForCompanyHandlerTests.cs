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
[TestCategory("Employee - GetEmployeeByIdForCompanyHandler Unit Tests")]
public class GetEmployeeByIdForCompanyHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryEmployee> _employeeRepositoryMock = default!;
    private Mock<IValidator<GetEmployeeByIdForCompanyQuery>> _validatorMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetEmployeeByIdForCompanyHandler>> _loggerMock = default!;
    private GetEmployeeByIdForCompanyHandler _handler = default!;

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

        _validatorMock = new Mock<IValidator<GetEmployeeByIdForCompanyQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetEmployeeByIdForCompanyQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<DetailEmployeeDTO>(It.IsAny<Employee>()))
            .Returns(default(DetailEmployeeDTO)!);

        _loggerMock = new Mock<ILogger<GetEmployeeByIdForCompanyHandler>>();

        _handler = new GetEmployeeByIdForCompanyHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _employeeRepositoryMock.Object,
            _validatorMock.Object,
            _mapperMock.Object,
            _loggerMock.Object
        );
    }

    private static GetEmployeeByIdForCompanyQuery CreateValidQuery()
    {
        return new GetEmployeeByIdForCompanyQuery(
            EmployeeId: Guid.NewGuid()
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

    private static Employee CreateEmployee(Guid employeeId, Guid companyId)
    {
        Guid identityUserId = Guid.NewGuid();

        var identityUser = new User
        {
            Id = identityUserId,
            UserName = "employeeUser",
            Email = "employee@example.com",
            UserType = UserType.Employee,
            CompanyId = companyId
        };

        return new Employee(
            employeeId,
            companyId,
            identityUser,
            identityUserId,
            "Joao da Silva",
            new DateOnly(2020, 1, 1),
            2000m
        );
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetEmployeeByIdForCompanyQuery query = CreateValidQuery();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetEmployeeByIdForCompanyQuery.EmployeeId), "O identificador do funcionário é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<DetailEmployeeDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailEmployeeDTO>(It.IsAny<Employee>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetEmployeeByIdForCompanyQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<DetailEmployeeDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailEmployeeDTO>(It.IsAny<Employee>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetEmployeeByIdForCompanyQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<DetailEmployeeDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailEmployeeDTO>(It.IsAny<Employee>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Company()
    {
        // arrange
        GetEmployeeByIdForCompanyQuery query = CreateValidQuery();

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
        Result<DetailEmployeeDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailEmployeeDTO>(It.IsAny<Employee>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Employee_Is_Not_Found()
    {
        // arrange
        GetEmployeeByIdForCompanyQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(query.EmployeeId))
            .ReturnsAsync((Employee?)null);

        // act
        Result<DetailEmployeeDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.EmployeeId), Times.Once);
        _mapperMock.Verify(m =>
            m.Map<DetailEmployeeDTO>(It.IsAny<Employee>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Employee_Belongs_To_Other_Company()
    {
        // arrange
        GetEmployeeByIdForCompanyQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Guid otherCompanyId = Guid.NewGuid();
        Employee employeeFromOtherCompany = CreateEmployee(query.EmployeeId, otherCompanyId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(query.EmployeeId))
            .ReturnsAsync(employeeFromOtherCompany);

        // act
        Result<DetailEmployeeDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        Assert.AreNotEqual(currentCompanyId, employeeFromOtherCompany.CompanyId);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.EmployeeId), Times.Once);
        _mapperMock.Verify(m =>
            m.Map<DetailEmployeeDTO>(It.IsAny<Employee>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InternalException_When_Exception_Occurs()
    {
        // arrange
        GetEmployeeByIdForCompanyQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(query.EmployeeId))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<DetailEmployeeDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a consulta de funcionário por Id")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailEmployeeDTO>(It.IsAny<Employee>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Employee_Detail_When_Request_Is_Valid()
    {
        // arrange
        GetEmployeeByIdForCompanyQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Employee employee = CreateEmployee(query.EmployeeId, currentCompanyId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(query.EmployeeId))
            .ReturnsAsync(employee);

        var expectedDto = new DetailEmployeeDTO
        (
           employee.Id,
           employee.Name,
           employee.HireDate,
           employee.Salary
        );

        _mapperMock
            .Setup(m => m.Map<DetailEmployeeDTO>(employee))
            .Returns(expectedDto);

        // act
        Result<DetailEmployeeDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);
        Assert.AreEqual(employee.Id, result.Value.Id);
        Assert.AreEqual(employee.Name, result.Value.Name);
        Assert.AreEqual(employee.HireDate, result.Value.HireDate);
        Assert.AreEqual(employee.Salary, result.Value.Salary);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.EmployeeId), Times.Once);
        _mapperMock.Verify(m =>
            m.Map<DetailEmployeeDTO>(employee), Times.Once);
    }
}