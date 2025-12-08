using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.BillingPlanModule.Commands;
using OblivionDrive.Application.BillingPlanModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.BillingPlanModule.HandlerTests;

[TestClass]
[TestCategory("BillingPlan - DeleteBillingPlanHandler Unit Tests")]
public class DeleteBillingPlanHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryBillingPlan> _billingPlanRepositoryMock = default!;
    private Mock<IValidator<DeleteBillingPlanCommand>> _validatorMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<DeleteBillingPlanHandler>> _loggerMock = default!;
    private DeleteBillingPlanHandler _handler = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;

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

        _billingPlanRepositoryMock = new Mock<IRepositoryBillingPlan>();
        _billingPlanRepositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<BillingPlan>()))
            .ReturnsAsync(true);

        _validatorMock = new Mock<IValidator<DeleteBillingPlanCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<DeleteBillingPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<DeleteBillingPlanHandler>>();

        _rentalRepositoryMock = new Mock<IRepositoryRental>();

        _handler = new DeleteBillingPlanHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _billingPlanRepositoryMock.Object,
            _rentalRepositoryMock.Object,
            _validatorMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private static DeleteBillingPlanCommand CreateValidCommand()
        => new DeleteBillingPlanCommand(Guid.NewGuid());

    private static User CreateCompanyUser(Guid id, Guid? companyId = null)
        => new()
        {
            Id = id,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? id
        };

    private static BillingPlan CreateBillingPlan(Guid companyId, Guid? vehicleGroupId = null)
    {
        Guid vgId = vehicleGroupId ?? Guid.NewGuid();

        var daily = new DailyBillingPlanConfig(100m, 1.5m);
        var controlled = new ControlledBillingPlanConfig(80m, 2.0m);
        var free = new FreeBillingPlanConfig(200m);

        return new BillingPlan(
            name: "Plano de cobrança teste",
            companyId: companyId,
            vehicleGroupId: vgId,
            dailyPlan: daily,
            controlledPlan: controlled,
            freePlan: free);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        DeleteBillingPlanCommand command = CreateValidCommand();

        var failures = new List<ValidationFailure>
        {
            new(nameof(DeleteBillingPlanCommand.BillingPlanId), "O identificador do plano de cobrança é obrigatório.")
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
        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<BillingPlan>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        DeleteBillingPlanCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<BillingPlan>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        DeleteBillingPlanCommand command = CreateValidCommand();

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

        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<BillingPlan>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_BillingPlan_Does_Not_Exist()
    {
        // arrange
        DeleteBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(command.BillingPlanId))
            .ReturnsAsync((BillingPlan?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.BillingPlanId), Times.Once);
        _billingPlanRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<BillingPlan>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_BillingPlan_Belongs_To_Other_Company()
    {
        // arrange
        DeleteBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        BillingPlan billingPlanFromOtherCompany = CreateBillingPlan(otherCompanyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(command.BillingPlanId))
            .ReturnsAsync(billingPlanFromOtherCompany);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.BillingPlanId), Times.Once);
        _billingPlanRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<BillingPlan>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Delete_BillingPlan_And_Return_Success()
    {
        // arrange
        DeleteBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        BillingPlan existingBillingPlan = CreateBillingPlan(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(command.BillingPlanId))
            .ReturnsAsync(existingBillingPlan);

        _billingPlanRepositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<BillingPlan>()))
            .ReturnsAsync(true);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.BillingPlanId), Times.Once);
        _billingPlanRepositoryMock.Verify(r =>
            r.DeleteAsync(existingBillingPlan), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        DeleteBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        BillingPlan existingBillingPlan = CreateBillingPlan(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(command.BillingPlanId))
            .ReturnsAsync(existingBillingPlan);

        _billingPlanRepositoryMock
            .Setup(r => r.DeleteAsync(existingBillingPlan))
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
                It.Is<It.IsAnyType>((value, _) =>
                    value.ToString()!.Contains("Ocorreu um erro durante a exclusão de plano de cobrança")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}