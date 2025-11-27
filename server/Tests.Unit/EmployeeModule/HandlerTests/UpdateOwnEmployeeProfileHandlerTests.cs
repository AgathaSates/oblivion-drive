using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.EmployeeModule.Commands;
using OblivionDrive.Application.EmployeeModule.DTOs;
using OblivionDrive.Application.EmployeeModule.Handlers;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.EmployeeModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.EmployeeModule.HandlerTests;

[TestClass]
[TestCategory("Employee - UpdateOwnEmployeeProfileHandler Unit Tests")]
public class UpdateOwnEmployeeProfileHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryEmployee> _employeeRepositoryMock = default!;
    private Mock<IValidator<UpdateOwnEmployeeProfileCommand>> _validatorMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<UpdateOwnEmployeeProfileHandler>> _loggerMock = default!;
    private UpdateOwnEmployeeProfileHandler _handler = default!;

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

        _employeeRepositoryMock
            .Setup(r => r.UpdateOwnProfileNameAsync(It.IsAny<Employee>(), It.IsAny<string>()))
            .ReturnsAsync((Employee employee, string _) => employee);

        _validatorMock = new Mock<IValidator<UpdateOwnEmployeeProfileCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateOwnEmployeeProfileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<UpdatedEmployeeDTO>(It.IsAny<Employee>()))
            .Returns(default(UpdatedEmployeeDTO)!);

        _loggerMock = new Mock<ILogger<UpdateOwnEmployeeProfileHandler>>();

        _handler = new UpdateOwnEmployeeProfileHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _employeeRepositoryMock.Object,
            _validatorMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object
        );
    }

    private static UpdateOwnEmployeeProfileCommand CreateValidCommand()
    {
        return new UpdateOwnEmployeeProfileCommand(
            Name: "Joao da Silva"
        );
    }

    private static User CreateEmployeeUser(Guid userId, Guid employeeId)
    {
        return new User
        {
            Id = userId,
            UserName = "employeeUser",
            Email = "employee@example.com",
            UserType = UserType.Employee,
            EmployeeID = employeeId
        };
    }

    private static Employee CreateEmployee(Guid employeeId, Guid identityUserId)
    {
        var identityUser = new User
        {
            Id = identityUserId,
            UserName = "employeeUser",
            Email = "employee@example.com",
            UserType = UserType.Employee
        };

        return new Employee(
            employeeId,
            Guid.NewGuid(),
            identityUser,
            identityUserId,
            "Nome Original",
            new DateOnly(2020, 1, 1),
            2000m
        );
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        var command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(UpdateOwnEmployeeProfileCommand.Name), "O nome do funcionário é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<UpdatedEmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.UpdateOwnProfileNameAsync(It.IsAny<Employee>(), It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        var command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<UpdatedEmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.UpdateOwnProfileNameAsync(It.IsAny<Employee>(), It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        var command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<UpdatedEmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.UpdateOwnProfileNameAsync(It.IsAny<Employee>(), It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Employee()
    {
        // arrange
        var command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        var currentUser = new User
        {
            Id = currentUserId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company
        };

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(currentUser);

        // act
        Result<UpdatedEmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.UpdateOwnProfileNameAsync(It.IsAny<Employee>(), It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Employee_Is_Not_Found()
    {
        // arrange
        var command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid employeeId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User employeeUser = CreateEmployeeUser(currentUserId, employeeId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(employeeUser);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId))
            .ReturnsAsync((Employee?)null);

        // act
        Result<UpdatedEmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(employeeId), Times.Once);
        _employeeRepositoryMock.Verify(r =>
            r.UpdateOwnProfileNameAsync(It.IsAny<Employee>(), It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Employee_Belongs_To_Other_User()
    {
        // arrange
        var command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid employeeId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User employeeUser = CreateEmployeeUser(currentUserId, employeeId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(employeeUser);

        Guid otherUserId = Guid.NewGuid();
        Employee employee = CreateEmployee(employeeId, otherUserId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId))
            .ReturnsAsync(employee);

        // act
        Result<UpdatedEmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        Assert.AreNotEqual(currentUserId, employee.IdentityUserId);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(employeeId), Times.Once);
        _employeeRepositoryMock.Verify(r =>
            r.UpdateOwnProfileNameAsync(It.IsAny<Employee>(), It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        var command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid employeeId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User employeeUser = CreateEmployeeUser(currentUserId, employeeId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(employeeUser);

        Employee employee = CreateEmployee(employeeId, currentUserId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId))
            .ReturnsAsync(employee);

        _employeeRepositoryMock
            .Setup(r => r.UpdateOwnProfileNameAsync(employee, It.IsAny<string>()))
            .ThrowsAsync(new Exception("Erro ao atualizar perfil"));

        // act
        Result<UpdatedEmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a atualização do perfil do funcionário")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Update_Profile_With_Correct_Data_And_Return_Success()
    {
        // arrange
        var command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid employeeId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User employeeUser = CreateEmployeeUser(currentUserId, employeeId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(employeeUser);

        Employee existingEmployee = CreateEmployee(employeeId, currentUserId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId))
            .ReturnsAsync(existingEmployee);

        string? capturedNewName = null;

        _employeeRepositoryMock
            .Setup(r => r.UpdateOwnProfileNameAsync(existingEmployee, It.IsAny<string>()))
             .Callback<Employee, string>((employee, newName) =>
             {
                 capturedNewName = newName;
                 employee.UpdateOwnProfileName(newName);
             })
             .ReturnsAsync(existingEmployee);


        var expectedDto = new UpdatedEmployeeDTO
        {
            UpdatedSuccessfully = true,
            Name = NameFormatter.FormatName(command.Name),
            HireDate = existingEmployee.HireDate,
            Salary = existingEmployee.Salary
        };

        _mapperMock
            .Setup(m => m.Map<UpdatedEmployeeDTO>(existingEmployee))
            .Returns(expectedDto);

        // act
        Result<UpdatedEmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert – resultado
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);
        Assert.IsTrue(result.Value.UpdatedSuccessfully);
        Assert.AreEqual(expectedDto.Name, result.Value.Name);
        Assert.AreEqual(expectedDto.HireDate, result.Value.HireDate);
        Assert.AreEqual(expectedDto.Salary, result.Value.Salary);

        string expectedFormattedName = NameFormatter.FormatName(command.Name);
        Assert.AreEqual(expectedFormattedName, capturedNewName);
        Assert.AreEqual(expectedFormattedName, existingEmployee.Name);

        _employeeRepositoryMock.Verify(r =>
            r.UpdateOwnProfileNameAsync(existingEmployee, It.IsAny<string>()), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }
}