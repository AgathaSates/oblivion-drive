using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.ServicesModule.Commands;
using OblivionDrive.Application.ServicesModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.ServicesModule.HandlerTests;

[TestClass]
[TestCategory("Service - DeleteServiceHandler Unit Tests")]
public class DeleteServiceHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryServices> _serviceRepositoryMock = default!;
    private Mock<IValidator<DeleteServiceCommand>> _validatorMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<DeleteServiceHandler>> _loggerMock = default!;
    private DeleteServiceHandler _handler = default!;

    [TestInitialize]
    public void Setup()
    {
        // UserManager<User> mock (padrão Identity, igual aos outros handlers)
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

        _serviceRepositoryMock = new Mock<IRepositoryServices>();
        _serviceRepositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<Service>()))
            .ReturnsAsync(true);

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<DeleteServiceCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<DeleteServiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<DeleteServiceHandler>>();

        _handler = new DeleteServiceHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _serviceRepositoryMock.Object,
            _validatorMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private static DeleteServiceCommand CreateValidCommand()
        => new DeleteServiceCommand(Guid.NewGuid());

    private static User CreateCompanyUser(Guid id, Guid? companyId = null)
        => new User
        {
            Id = id,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? id
        };

    private static Service CreateService(Guid companyId)
        => new Service(
            name: "Serviço para exclusão",
            price: 100m,
            chargeType: (ChargeType)1,
            companyId: companyId);

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        DeleteServiceCommand command = CreateValidCommand();

        var failures = new List<ValidationFailure>
        {
            new(nameof(DeleteServiceCommand.ServiceId), "O identificador do serviço é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Service>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        DeleteServiceCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Service>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        DeleteServiceCommand command = CreateValidCommand();

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

        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Service>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Service_Does_Not_Exist()
    {
        // arrange
        DeleteServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ServiceId))
            .ReturnsAsync((Service?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ServiceId), Times.Once);
        _serviceRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Service>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Service_Belongs_To_Other_Company()
    {
        // arrange
        DeleteServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Service serviceFromOtherCompany = CreateService(otherCompanyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ServiceId))
            .ReturnsAsync(serviceFromOtherCompany);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ServiceId), Times.Once);
        _serviceRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Service>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Delete_Service_And_Return_Success()
    {
        // arrange
        DeleteServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Service existingService = CreateService(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ServiceId))
            .ReturnsAsync(existingService);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ServiceId), Times.Once);
        _serviceRepositoryMock.Verify(r =>
            r.DeleteAsync(existingService), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        DeleteServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Service existingService = CreateService(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ServiceId))
            .ReturnsAsync(existingService);

        _serviceRepositoryMock
            .Setup(r => r.DeleteAsync(existingService))
            .ThrowsAsync(new Exception("Erro de banco"));

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
                    v.ToString()!.Contains("Ocorreu um erro durante a exclusão de serviço")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}