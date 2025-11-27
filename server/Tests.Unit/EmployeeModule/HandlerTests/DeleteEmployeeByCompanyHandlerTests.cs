using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.EmployeeModule.Commands;
using OblivionDrive.Application.EmployeeModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.EmployeeModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.EmployeeModule.HandlerTests;

[TestClass]
[TestCategory("Employee - DeleteEmployeeByCompanyHandler Unit Tests")]
public class DeleteEmployeeByCompanyHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryEmployee> _employeeRepositoryMock = default!;
    private Mock<IValidator<DeleteEmployeeByCompanyCommand>> _validatorMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<DeleteEmployeeByCompanyHandler>> _loggerMock = default!;
    private DeleteEmployeeByCompanyHandler _handler = default!;

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
            .Setup(r => r.DeleteAsync(It.IsAny<Employee>()))
            .ReturnsAsync(true);

        _validatorMock = new Mock<IValidator<DeleteEmployeeByCompanyCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<DeleteEmployeeByCompanyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<DeleteEmployeeByCompanyHandler>>();

        _handler = new DeleteEmployeeByCompanyHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _employeeRepositoryMock.Object,
            _validatorMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private static DeleteEmployeeByCompanyCommand CreateValidCommand()
    {
        return new DeleteEmployeeByCompanyCommand(
            EmployeeId: Guid.NewGuid()
        );
    }

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
    {
        Guid effectiveCompanyId = companyId ?? userId;

        return new User
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = effectiveCompanyId
        };
    }

    private static Employee CreateEmployee(Guid employeeId, Guid companyId, Guid identityUserId)
    {
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
        DeleteEmployeeByCompanyCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(DeleteEmployeeByCompanyCommand.EmployeeId),
                "O identificador do funcionário é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

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
            r.DeleteAsync(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        DeleteEmployeeByCompanyCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        DeleteEmployeeByCompanyCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Company()
    {
        // arrange
        DeleteEmployeeByCompanyCommand command = CreateValidCommand();

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
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _employeeRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Employee>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Employee_Is_Not_Found()
    {
        // arrange
        DeleteEmployeeByCompanyCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(command.EmployeeId))
            .ReturnsAsync((Employee?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.EmployeeId), Times.Once);
        _employeeRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Employee>()), Times.Never);
        _userManagerMock.Verify(m =>
            m.DeleteAsync(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Employee_Belongs_To_Other_Company()
    {
        // arrange
        DeleteEmployeeByCompanyCommand command = CreateValidCommand();

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
        Employee employeeFromOtherCompany = CreateEmployee(
            command.EmployeeId,
            otherCompanyId,
            Guid.NewGuid()
        );

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(command.EmployeeId))
            .ReturnsAsync(employeeFromOtherCompany);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        Assert.AreNotEqual(currentCompanyId, employeeFromOtherCompany.CompanyId);

        _employeeRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.EmployeeId), Times.Once);
        _employeeRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Employee>()), Times.Never);
        _userManagerMock.Verify(m =>
            m.DeleteAsync(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Delete_Employee_Without_User_And_Commit_When_IdentityUserId_Is_Empty()
    {
        // arrange
        DeleteEmployeeByCompanyCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Guid employeeId = command.EmployeeId;
        Guid identityUserId = Guid.Empty;

        Employee employee = CreateEmployee(employeeId, currentCompanyId, identityUserId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId))
            .ReturnsAsync(employee);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        _employeeRepositoryMock.Verify(r =>
            r.DeleteAsync(employee), Times.Once);

        _userManagerMock.Verify(m =>
            m.DeleteAsync(It.IsAny<User>()), Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(employee.IdentityUserId.ToString()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Delete_Employee_And_User_And_Commit_When_DeleteUser_Succeeds()
    {
        // arrange
        DeleteEmployeeByCompanyCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Guid employeeId = command.EmployeeId;
        Guid identityUserId = Guid.NewGuid();

        Employee employee = CreateEmployee(employeeId, currentCompanyId, identityUserId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId))
            .ReturnsAsync(employee);

        var employeeUser = new User
        {
            Id = identityUserId,
            UserName = "employeeUser",
            Email = "employee@example.com",
            UserType = UserType.Employee
        };

        _userManagerMock
            .Setup(m => m.FindByIdAsync(identityUserId.ToString()))
            .ReturnsAsync(employeeUser);

        _userManagerMock
            .Setup(m => m.DeleteAsync(employeeUser))
            .ReturnsAsync(IdentityResult.Success);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        _employeeRepositoryMock.Verify(r =>
            r.DeleteAsync(employee), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(identityUserId.ToString()), Times.Once);

        _userManagerMock.Verify(m =>
            m.DeleteAsync(employeeUser), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_DeleteUser_Fails()
    {
        // arrange
        DeleteEmployeeByCompanyCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Guid employeeId = command.EmployeeId;
        Guid identityUserId = Guid.NewGuid();

        Employee employee = CreateEmployee(employeeId, currentCompanyId, identityUserId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId))
            .ReturnsAsync(employee);

        var employeeUser = new User
        {
            Id = identityUserId,
            UserName = "employeeUser",
            Email = "employee@example.com",
            UserType = UserType.Employee
        };

        _userManagerMock
            .Setup(m => m.FindByIdAsync(identityUserId.ToString()))
            .ReturnsAsync(employeeUser);

        IdentityError[] deleteUserErrors =
        [
            new IdentityError { Code = "DeleteFailed", Description = "Erro ao excluir usuário identity." }
        ];

        _userManagerMock
            .Setup(m => m.DeleteAsync(employeeUser))
            .ReturnsAsync(IdentityResult.Failed(deleteUserErrors));

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _employeeRepositoryMock.Verify(r =>
            r.DeleteAsync(employee), Times.Once);

        _userManagerMock.Verify(m =>
            m.DeleteAsync(employeeUser), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        DeleteEmployeeByCompanyCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Guid employeeId = command.EmployeeId;
        Guid identityUserId = Guid.NewGuid();

        Employee employee = CreateEmployee(employeeId, currentCompanyId, identityUserId);

        _employeeRepositoryMock
            .Setup(r => r.GetByIdAsync(employeeId))
            .ReturnsAsync(employee);

        _employeeRepositoryMock
            .Setup(r => r.DeleteAsync(employee))
            .ThrowsAsync(new Exception("Erro de banco ao excluir funcionário"));

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante a exclusão de funcionário")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}