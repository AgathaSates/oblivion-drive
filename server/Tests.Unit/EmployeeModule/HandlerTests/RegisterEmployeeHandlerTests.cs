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
[TestCategory("Employee - RegisterEmployeeHandler Unit Tests")]
public class RegisterEmployeeHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<IValidator<RegisterEmployeeCommand>> _validatorMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryEmployee> _employeeRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<RegisterEmployeeCommand>> _loggerMock = default!;
    private RegisterEmployeeHandler _handler = default!;

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

        _validatorMock = new Mock<IValidator<RegisterEmployeeCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterEmployeeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(Guid.NewGuid());

        _employeeRepositoryMock = new Mock<IRepositoryEmployee>();
        _employeeRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Employee>()))
            .ReturnsAsync(Guid.NewGuid());

        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<EmployeeDTO>(It.IsAny<Employee>()))
            .Returns(default(EmployeeDTO)!);

        _loggerMock = new Mock<ILogger<RegisterEmployeeCommand>>();

        _handler = new RegisterEmployeeHandler(
            _userManagerMock.Object,
            _validatorMock.Object,
            _tenantProviderMock.Object,
            _employeeRepositoryMock.Object,
            _loggerMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );
    }

    private static RegisterEmployeeCommand CreateValidCommand()
    {
        return new RegisterEmployeeCommand(
            UserName: "validUser",
            Email: "user@example.com",
            Password: "Senha123!",
            Name: "Joao da Silva",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3000m
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

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        RegisterEmployeeCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(RegisterEmployeeCommand.UserName), "O nome de usuário é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<EmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        RegisterEmployeeCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<EmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        RegisterEmployeeCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<EmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _employeeRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Company()
    {
        // arrange
        RegisterEmployeeCommand command = CreateValidCommand();

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
        Result<EmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _userManagerMock.Verify(m =>
            m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(m =>
            m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_CreateUser_Fails()
    {
        // arrange
        RegisterEmployeeCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        IdentityError[] identityErrors =
        [
            new IdentityError { Code = "CreateFailed", Description = "Falha ao criar usuário." }
        ];

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // act
        Result<EmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.CreateAsync(It.IsAny<User>(), command.Password), Times.Once);

        _userManagerMock.Verify(m =>
            m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_AddToRole_Fails()
    {
        // arrange
        RegisterEmployeeCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        IdentityError[] roleErrors =
        [
            new IdentityError { Code = "AddToRoleFailed", Description = "Falha ao adicionar role Employee." }
        ];

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), UserType.Employee.ToString()))
            .ReturnsAsync(IdentityResult.Failed(roleErrors));

        // act
        Result<EmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.CreateAsync(It.IsAny<User>(), command.Password), Times.Once);

        _userManagerMock.Verify(m =>
            m.AddToRoleAsync(It.IsAny<User>(), UserType.Employee.ToString()), Times.Once);

        _employeeRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Success_When_Employee_Is_Created()
    {
        // arrange
        RegisterEmployeeCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), UserType.Employee.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        var expectedEmployeeDto = default(EmployeeDTO)!;
        _mapperMock
            .Setup(m => m.Map<EmployeeDTO>(It.IsAny<Employee>()))
            .Returns(expectedEmployeeDto);

        // act
        Result<EmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _userManagerMock.Verify(m =>
            m.CreateAsync(It.IsAny<User>(), command.Password), Times.Once);

        _userManagerMock.Verify(m =>
            m.AddToRoleAsync(It.IsAny<User>(), UserType.Employee.ToString()), Times.Once);

        _employeeRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Employee>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        RegisterEmployeeCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), UserType.Employee.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        _employeeRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Employee>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<EmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante o registro de funcionário")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Create_IdentityUser_And_Employee_With_Correct_Data()
    {
        // arrange
        var command = CreateValidCommand();

        var currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        var companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        User? capturedIdentityUser = null;
        Employee? capturedEmployee = null;

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .Callback<User, string>((user, _) => capturedIdentityUser = user)
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), UserType.Employee.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        _employeeRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Employee>()))
            .Callback<Employee>(e => capturedEmployee = e)
            .ReturnsAsync(Guid.NewGuid());

        var expectedDto = new EmployeeDTO
        (
            true,
            NameFormatter.FormatName(command.Name),
            command.UserName
        );

        _mapperMock
            .Setup(m => m.Map<EmployeeDTO>(It.IsAny<Employee>()))
            .Returns(expectedDto);

        // act
        Result<EmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert – resultado
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        // assert 
        Assert.IsNotNull(capturedIdentityUser);
        Assert.AreEqual(command.UserName, capturedIdentityUser!.UserName);
        Assert.AreEqual(command.Email, capturedIdentityUser.Email);
        Assert.AreEqual(UserType.Employee, capturedIdentityUser.UserType);
        Assert.AreEqual(companyUser.CompanyId ?? companyUser.Id, capturedIdentityUser.CompanyId);
        Assert.AreEqual(companyUser, capturedIdentityUser.CompanyUser);
        Assert.AreNotEqual(Guid.Empty, capturedIdentityUser.EmployeeID);

        Assert.IsNotNull(capturedEmployee);
        Assert.AreEqual(capturedIdentityUser.EmployeeID, capturedEmployee!.Id);
        Assert.AreEqual(capturedIdentityUser.CompanyId, capturedEmployee.CompanyId);
        Assert.AreEqual(NameFormatter.FormatName(command.Name), capturedEmployee.Name);
        Assert.AreEqual(command.HireDate, capturedEmployee.HireDate);
        Assert.AreEqual(command.Salary, capturedEmployee.Salary);

        _userManagerMock.Verify(m =>
            m.CreateAsync(It.IsAny<User>(), command.Password), Times.Once);

        _userManagerMock.Verify(m =>
            m.AddToRoleAsync(It.IsAny<User>(), UserType.Employee.ToString()), Times.Once);

        _employeeRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Employee>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_UserName_Already_Exists()
    {
        // arrange
        RegisterEmployeeCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = command.UserName,
            Email = "other@example.com",
            UserType = UserType.Employee
        };

        _userManagerMock
            .Setup(m => m.FindByNameAsync(command.UserName))
            .ReturnsAsync(existingUser);

        // act
        Result<EmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        var error = result.Errors.Single();

        Assert.IsTrue(
            error.Metadata.TryGetValue("ErrorType", out object? errorType) &&
            string.Equals(errorType?.ToString(), "InvalidRequest", StringComparison.Ordinal),
            "ErrorType deveria ser 'InvalidRequest'."
        );

        Assert.IsTrue(
            error.Reasons.Any(reason =>
                reason.Message.Contains("Já existe um usuário cadastrado com este nome de usuário.", StringComparison.CurrentCulture)),
            "Deveria conter a mensagem de username duplicado."
        );

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByNameAsync(command.UserName), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByEmailAsync(It.IsAny<string>()), Times.Never);

        _employeeRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>()), Times.Never);

        _userManagerMock.Verify(m =>
            m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);

        _userManagerMock.Verify(m =>
            m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);

        _employeeRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Employee>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Email_Already_Exists()
    {
        // arrange
        RegisterEmployeeCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _userManagerMock
            .Setup(m => m.FindByNameAsync(command.UserName))
            .ReturnsAsync((User?)null);

        var existingUserByEmail = new User
        {
            Id = Guid.NewGuid(),
            UserName = "otheruser",
            Email = command.Email,
            UserType = UserType.Employee
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync(existingUserByEmail);

        // act
        Result<EmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        var error = result.Errors.Single();

        Assert.IsTrue(
            error.Metadata.TryGetValue("ErrorType", out object? errorType) &&
            string.Equals(errorType?.ToString(), "InvalidRequest", StringComparison.Ordinal),
            "ErrorType deveria ser 'InvalidRequest'."
        );

        Assert.IsTrue(
            error.Reasons.Any(reason =>
                reason.Message.Contains("Já existe um usuário cadastrado com este e-mail.", StringComparison.CurrentCulture)),
            "Deveria conter a mensagem de e-mail duplicado."
        );

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByNameAsync(command.UserName), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByEmailAsync(command.Email), Times.Once);

        _employeeRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>()), Times.Never);

        _userManagerMock.Verify(m =>
            m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);

        _userManagerMock.Verify(m =>
            m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);

        _employeeRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Employee>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Employee_Name_Already_Exists()
    {
        // arrange
        RegisterEmployeeCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _userManagerMock
            .Setup(m => m.FindByNameAsync(command.UserName))
            .ReturnsAsync((User?)null);

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);

        _employeeRepositoryMock
            .Setup(r => r.ExistsByNameAsync(command.Name))
            .ReturnsAsync(true);

        // act
        Result<EmployeeDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        var error = result.Errors.Single();

        Assert.IsTrue(
            error.Metadata.TryGetValue("ErrorType", out object? errorType) &&
            string.Equals(errorType?.ToString(), "InvalidRequest", StringComparison.Ordinal),
            "ErrorType deveria ser 'InvalidRequest'."
        );

        Assert.IsTrue(
            error.Reasons.Any(reason =>
                reason.Message.Contains("Já existe um funcionário cadastrado com este nome", StringComparison.CurrentCulture)),
            "Deveria conter a mensagem de nome de funcionário duplicado."
        );

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByNameAsync(command.UserName), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByEmailAsync(command.Email), Times.Once);

        _employeeRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(command.Name), Times.Once);

        _userManagerMock.Verify(m =>
            m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);

        _userManagerMock.Verify(m =>
            m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);

        _employeeRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Employee>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }
}