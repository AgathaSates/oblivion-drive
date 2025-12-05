using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.BillingPlanModule.Commands;
using OblivionDrive.Application.BillingPlanModule.DTOs;
using OblivionDrive.Application.BillingPlanModule.Handlers;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.BillingPlanModule.HandlerTests;

[TestClass]
[TestCategory("BillingPlan - RegisterBillingPlanHandler Unit Tests")]
public class RegisterBillingPlanHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryBillingPlan> _billingPlanRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IValidator<RegisterBillingPlanCommand>> _validatorMock = default!;
    private Mock<ILogger<RegisterBillingPlanCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private RegisterBillingPlanHandler _handler = default!;

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
            .Setup(r => r.ExistsByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _billingPlanRepositoryMock
            .Setup(r => r.ExistsForVehicleGroupAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);
        _billingPlanRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<BillingPlan>()))
            .ReturnsAsync(Guid.NewGuid());

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<RegisterBillingPlanCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterBillingPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<RegisterBillingPlanCommand>>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<BillingPlanDTO>(It.IsAny<BillingPlan>()))
            .Returns(default(BillingPlanDTO)!);

        _handler = new RegisterBillingPlanHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _billingPlanRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    private static RegisterBillingPlanCommand CreateValidCommand()
    {
        return new RegisterBillingPlanCommand(
            Name: "Plano Ouro",
            VehicleGroupId: Guid.NewGuid(),
            DailyPlanDailyRate: 100m,
            DailyPlanPricePerKilometer: 1.50m,
            ControlledPlanDailyRate: 80m,
            ControlledPlanExtraPricePerKilometer: 2.00m,
            FreePlanDailyRate: 200m
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

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(RegisterBillingPlanCommand.Name), "O nome do plano de cobrança é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<BillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<BillingPlan>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<BillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<BillingPlan>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<BillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<BillingPlan>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_BillingPlan_Name_Already_Exists()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // act
        Result<BillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed, "Resultado deveria ser falha quando o nome do plano já existe.");

        var error = result.Errors.Single();

        Assert.IsTrue(
            error.Metadata.TryGetValue("ErrorType", out object? errorType) &&
            string.Equals(errorType?.ToString(), "InvalidRequest", StringComparison.Ordinal),
            "ErrorType deveria ser 'InvalidRequest'."
        );

        Assert.IsTrue(
            error.Reasons.Any(r =>
                r.Message.Contains("Já existe um plano de cobrança cadastrado com este nome para esta empresa.",
                    StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um plano com este nome para esta empresa."
        );

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>()), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<BillingPlan>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_BillingPlan_For_VehicleGroup_Already_Exists()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsForVehicleGroupAsync(command.VehicleGroupId))
            .ReturnsAsync(true);

        // act
        Result<BillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed, "Resultado deveria ser falha quando já existe plano para o grupo de veículos.");

        var error = result.Errors.Single();

        Assert.IsTrue(
            error.Metadata.TryGetValue("ErrorType", out object? errorType) &&
            string.Equals(errorType?.ToString(), "InvalidRequest", StringComparison.Ordinal),
            "ErrorType deveria ser 'InvalidRequest'."
        );

        Assert.IsTrue(
            error.Reasons.Any(r =>
                r.Message.Contains("Já existe um plano de cobrança cadastrado para este grupo de veículos.",
                    StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe plano para este grupo de veículos."
        );

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>()), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(command.VehicleGroupId), Times.Once);
        _billingPlanRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<BillingPlan>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Create_BillingPlan_And_Return_Success()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        BillingPlan? capturedBillingPlan = null;

        _billingPlanRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<BillingPlan>()))
            .Callback<BillingPlan>(bp => capturedBillingPlan = bp)
            .ReturnsAsync(Guid.NewGuid());

        string expectedFormattedName = NameFormatter.FormatName(command.Name);

        var expectedDto = new BillingPlanDTO(
            CreatedSuccessfully: true,
            Name: expectedFormattedName,
            VehicleGroupId: command.VehicleGroupId,
            DailyPlanDailyRate: command.DailyPlanDailyRate,
            DailyPlanPricePerKilometer: command.DailyPlanPricePerKilometer,
            ControlledPlanDailyRate: command.ControlledPlanDailyRate,
            ControlledPlanExtraPricePerKilometer: command.ControlledPlanExtraPricePerKilometer,
            FreePlanDailyRate: command.FreePlanDailyRate
        );

        _mapperMock
            .Setup(m => m.Map<BillingPlanDTO>(It.IsAny<BillingPlan>()))
            .Returns(expectedDto);

        // act
        Result<BillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(capturedBillingPlan);
        Assert.AreNotEqual(Guid.Empty, capturedBillingPlan!.Id);
        Assert.AreEqual(companyId, capturedBillingPlan.CompanyId);
        Assert.AreEqual(expectedFormattedName, capturedBillingPlan.Name);
        Assert.AreEqual(command.VehicleGroupId, capturedBillingPlan.VehicleGroupId);

        Assert.AreEqual(command.DailyPlanDailyRate, capturedBillingPlan.DailyPlan.DailyRate);
        Assert.AreEqual(command.DailyPlanPricePerKilometer, capturedBillingPlan.DailyPlan.PricePerKilometer);
        Assert.AreEqual(command.ControlledPlanDailyRate, capturedBillingPlan.ControlledPlan.DailyRate);
        Assert.AreEqual(command.ControlledPlanExtraPricePerKilometer, capturedBillingPlan.ControlledPlan.ExtraPricePerKilometer);
        Assert.AreEqual(command.FreePlanDailyRate, capturedBillingPlan.FreePlan.DailyRate);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>()), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(command.VehicleGroupId), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<BillingPlan>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<BillingPlan>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<BillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante o registro de plano de cobrança")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}