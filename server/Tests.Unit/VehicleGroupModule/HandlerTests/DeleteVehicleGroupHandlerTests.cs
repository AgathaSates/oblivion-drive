using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.VehicleGroupModule.commands;
using OblivionDrive.Application.VehicleGroupModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Tests.Unit.VehicleGroupModule.HandlerTests;

[TestClass]
[TestCategory("VehicleGroup - DeleteVehicleGroupHandler Unit Tests")]
public class DeleteVehicleGroupHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryVehicleGroup> _vehicleGroupRepositoryMock = default!;
    private Mock<IRepositoryBillingPlan> _billingPlanRepositoryMock = default!;
    private Mock<IValidator<DeleteVehicleGroupCommand>> _validatorMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<DeleteVehicleGroupHandler>> _loggerMock = default!;
    private DeleteVehicleGroupHandler _handler = default!;
    private Mock<IRepositoryVehicle> _vehicleRepositoryMock = default!;

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

        _vehicleGroupRepositoryMock = new Mock<IRepositoryVehicleGroup>();

        _billingPlanRepositoryMock = new Mock<IRepositoryBillingPlan>();
        _billingPlanRepositoryMock
            .Setup(r => r.ExistsForVehicleGroupAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _vehicleRepositoryMock = new Mock<IRepositoryVehicle>();

        _validatorMock = new Mock<IValidator<DeleteVehicleGroupCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<DeleteVehicleGroupCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<DeleteVehicleGroupHandler>>();

        _handler = new DeleteVehicleGroupHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _vehicleGroupRepositoryMock.Object,
            _billingPlanRepositoryMock.Object,
            _vehicleRepositoryMock.Object,
            _validatorMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private static DeleteVehicleGroupCommand CreateValidCommand()
    {
        return new DeleteVehicleGroupCommand(
            VehicleGroupId: Guid.NewGuid()
        );
    }

    private static User CreateCompanyUser(Guid id, Guid? companyId = null)
    {
        return new User
        {
            Id = id,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? id
        };
    }

    private static VehicleGroup CreateVehicleGroup(Guid companyId)
    {
        return new VehicleGroup(
            name: "Grupo de veículos teste",
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        DeleteVehicleGroupCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(DeleteVehicleGroupCommand.VehicleGroupId), "O identificador do grupo de veículos é obrigatório.")
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
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<VehicleGroup>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        DeleteVehicleGroupCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<VehicleGroup>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        DeleteVehicleGroupCommand command = CreateValidCommand();

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

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<VehicleGroup>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_VehicleGroup_Does_Not_Exist()
    {
        // arrange
        DeleteVehicleGroupCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync((VehicleGroup?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleGroupId), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<VehicleGroup>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_VehicleGroup_Belongs_To_Other_Company()
    {
        // arrange
        DeleteVehicleGroupCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        VehicleGroup otherCompanyVehicleGroup = CreateVehicleGroup(otherCompanyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(otherCompanyVehicleGroup);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<VehicleGroup>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_VehicleGroup_Is_Used_By_BillingPlans()
    {
        // arrange
        DeleteVehicleGroupCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        VehicleGroup existingVehicleGroup = CreateVehicleGroup(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(existingVehicleGroup);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsForVehicleGroupAsync(existingVehicleGroup.Id))
            .ReturnsAsync(true);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed, "Resultado deveria ser falha quando o grupo está vinculado a planos de cobrança.");

        var error = result.Errors.Single();

        Assert.IsTrue(
            error.Metadata.TryGetValue("ErrorType", out object? errorType) &&
            string.Equals(errorType?.ToString(), "InvalidRequest", StringComparison.Ordinal),
            "ErrorType deveria ser 'InvalidRequest'."
        );

        Assert.IsTrue(
            error.Reasons.Any(r =>
                r.Message.Contains("Não é permitido excluir grupos de veículos que estejam vinculados a planos de cobrança.",
                    StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que o grupo está vinculado a planos de cobrança."
        );

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(existingVehicleGroup.Id), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<VehicleGroup>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Delete_VehicleGroup_And_Return_Success()
    {
        // arrange
        DeleteVehicleGroupCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        VehicleGroup existingVehicleGroup = CreateVehicleGroup(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(existingVehicleGroup);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsForVehicleGroupAsync(existingVehicleGroup.Id))
            .ReturnsAsync(false);

        _vehicleGroupRepositoryMock
            .Setup(r => r.DeleteAsync(existingVehicleGroup))
            .ReturnsAsync(true);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleGroupId), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(existingVehicleGroup.Id), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.DeleteAsync(existingVehicleGroup), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        DeleteVehicleGroupCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        VehicleGroup existingVehicleGroup = CreateVehicleGroup(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(existingVehicleGroup);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsForVehicleGroupAsync(existingVehicleGroup.Id))
            .ReturnsAsync(false);

        _vehicleGroupRepositoryMock
            .Setup(r => r.DeleteAsync(existingVehicleGroup))
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
                    v.ToString()!.Contains("Ocorreu um erro durante a exclusão de grupo de veículos")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
