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
[TestCategory("BillingPlan - UpdateBillingPlanHandler Unit Tests")]
public class UpdateBillingPlanHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<IValidator<UpdateBillingPlanCommand>> _validatorMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryBillingPlan> _billingPlanRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<UpdateBillingPlanCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private UpdateBillingPlanHandler _handler = default!;

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

        _validatorMock = new Mock<IValidator<UpdateBillingPlanCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateBillingPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(Guid.NewGuid());

        _billingPlanRepositoryMock = new Mock<IRepositoryBillingPlan>();
        _billingPlanRepositoryMock
            .Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);
        _billingPlanRepositoryMock
            .Setup(r => r.ExistsForVehicleGroupAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<UpdateBillingPlanCommand>>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<UpdatedBillingPlanDTO>(It.IsAny<BillingPlan>()))
            .Returns(default(UpdatedBillingPlanDTO)!);

        _handler = new UpdateBillingPlanHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _billingPlanRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    private static UpdateBillingPlanCommand CreateValidCommand()
    {
        return new UpdateBillingPlanCommand(
            BillingPlanId: Guid.NewGuid(),
            Name: "Plano Ouro Atualizado",
            VehicleGroupId: Guid.NewGuid(),
            DailyPlanDailyRate: 120m,
            DailyPlanPricePerKilometer: 1.75m,
            ControlledPlanDailyRate: 90m,
            ControlledPlanExtraPricePerKilometer: 2.10m,
            FreePlanDailyRate: 220m
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

    private static BillingPlan CreateBillingPlan(Guid companyId, Guid vehicleGroupId, string name = "Plano Original")
    {
        var dailyPlanConfig = new DailyBillingPlanConfig(
            dailyRate: 100m,
            pricePerKilometer: 1.5m);

        var controlledPlanConfig = new ControlledBillingPlanConfig(
            dailyRate: 80m,
            extraPricePerKilometer: 2.0m);

        var freePlanConfig = new FreeBillingPlanConfig(
            dailyRate: 200m);

        return new BillingPlan(
            name,
            companyId,
            vehicleGroupId,
            dailyPlanConfig,
            controlledPlanConfig,
            freePlanConfig);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        UpdateBillingPlanCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(UpdateBillingPlanCommand.Name), "O nome do plano de cobrança é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<UpdatedBillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

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
            r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<BillingPlan>(), It.IsAny<BillingPlan>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        UpdateBillingPlanCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<UpdatedBillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<BillingPlan>(), It.IsAny<BillingPlan>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        UpdateBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<UpdatedBillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<BillingPlan>(), It.IsAny<BillingPlan>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_BillingPlan_Does_Not_Exist()
    {
        // arrange
        UpdateBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

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
        Result<UpdatedBillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.BillingPlanId), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<BillingPlan>(), It.IsAny<BillingPlan>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_BillingPlan_Belongs_To_Other_Company()
    {
        // arrange
        UpdateBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        BillingPlan otherCompanyPlan = CreateBillingPlan(otherCompanyId, vehicleGroupId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(command.BillingPlanId))
            .ReturnsAsync(otherCompanyPlan);

        // act
        Result<UpdatedBillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.BillingPlanId), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<BillingPlan>(), It.IsAny<BillingPlan>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_BillingPlan_Name_Already_Exists_For_Other_Plan()
    {
        // arrange
        UpdateBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        BillingPlan existingPlan = CreateBillingPlan(companyId, vehicleGroupId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(command.BillingPlanId))
            .ReturnsAsync(existingPlan);

        string formattedName = NameFormatter.FormatName(command.Name);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsByNameAsync(formattedName, command.BillingPlanId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedBillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed, "Deveria falhar quando já existe plano com o mesmo nome para outra entidade.");

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
            "Mensagem de erro deveria indicar duplicidade de nome para a empresa."
        );

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(formattedName, command.BillingPlanId), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<BillingPlan>(), It.IsAny<BillingPlan>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_BillingPlan_For_VehicleGroup_Already_Exists_For_Other_Plan()
    {
        // arrange
        UpdateBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = command.VehicleGroupId;

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        BillingPlan existingPlan = CreateBillingPlan(companyId, vehicleGroupId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(command.BillingPlanId))
            .ReturnsAsync(existingPlan);

        string formattedName = NameFormatter.FormatName(command.Name);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsByNameAsync(formattedName, command.BillingPlanId))
            .ReturnsAsync(false);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsForVehicleGroupAsync(command.VehicleGroupId, command.BillingPlanId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedBillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed, "Deveria falhar quando já existe outro plano para o mesmo grupo de veículos.");

        var error = result.Errors.Single();

        Assert.IsTrue(
            error.Metadata.TryGetValue("ErrorType", out object? errorType) &&
            string.Equals(errorType?.ToString(), "InvalidRequest", StringComparison.Ordinal),
            "ErrorType deveria ser 'InvalidRequest'."
        );

        Assert.IsTrue(
            error.Reasons.Any(r =>
                r.Message.Contains("Já existe outro plano de cobrança cadastrado para este grupo de veículos.",
                    StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar duplicidade para o grupo de veículos."
        );

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(formattedName, command.BillingPlanId), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(command.VehicleGroupId, command.BillingPlanId), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<BillingPlan>(), It.IsAny<BillingPlan>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Update_BillingPlan_And_Return_Success()
    {
        // arrange
        UpdateBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = command.VehicleGroupId;

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        BillingPlan existingPlan = CreateBillingPlan(companyId, vehicleGroupId, "Plano Original");

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(command.BillingPlanId))
            .ReturnsAsync(existingPlan);

        string expectedFormattedName = NameFormatter.FormatName(command.Name);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsByNameAsync(expectedFormattedName, command.BillingPlanId))
            .ReturnsAsync(false);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsForVehicleGroupAsync(command.VehicleGroupId, command.BillingPlanId))
            .ReturnsAsync(false);

        BillingPlan? capturedExisting = null;
        BillingPlan? capturedUpdatedData = null;
        BillingPlan? returnedPlan = null;

        _billingPlanRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<BillingPlan>(), It.IsAny<BillingPlan>()))
            .Callback<BillingPlan, BillingPlan>((existing, updatedData) =>
            {
                capturedExisting = existing;
                capturedUpdatedData = updatedData;

                existing.Update(updatedData);
                returnedPlan = existing;
            })
            .ReturnsAsync(() => returnedPlan!);

        var expectedDto = new UpdatedBillingPlanDTO(
            UpdatedSuccessfully: true,
            Name: expectedFormattedName,
            VehicleGroupId: command.VehicleGroupId,
            DailyPlanDailyRate: command.DailyPlanDailyRate,
            DailyPlanPricePerKilometer: command.DailyPlanPricePerKilometer,
            ControlledPlanDailyRate: command.ControlledPlanDailyRate,
            ControlledPlanExtraPricePerKilometer: command.ControlledPlanExtraPricePerKilometer,
            FreePlanDailyRate: command.FreePlanDailyRate
        );

        _mapperMock
            .Setup(m => m.Map<UpdatedBillingPlanDTO>(existingPlan))
            .Returns(expectedDto);

        // act
        Result<UpdatedBillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(capturedUpdatedData);
        Assert.AreEqual(expectedFormattedName, capturedUpdatedData!.Name);
        Assert.AreEqual(command.VehicleGroupId, capturedUpdatedData.VehicleGroupId);
        Assert.AreEqual(companyId, capturedUpdatedData.CompanyId);

        Assert.AreEqual(command.DailyPlanDailyRate, capturedUpdatedData.DailyPlan.DailyRate);
        Assert.AreEqual(command.DailyPlanPricePerKilometer, capturedUpdatedData.DailyPlan.PricePerKilometer);
        Assert.AreEqual(command.ControlledPlanDailyRate, capturedUpdatedData.ControlledPlan.DailyRate);
        Assert.AreEqual(command.ControlledPlanExtraPricePerKilometer, capturedUpdatedData.ControlledPlan.ExtraPricePerKilometer);
        Assert.AreEqual(command.FreePlanDailyRate, capturedUpdatedData.FreePlan.DailyRate);

        Assert.IsNotNull(capturedExisting);
        Assert.AreEqual(expectedFormattedName, capturedExisting!.Name);
        Assert.AreEqual(command.VehicleGroupId, capturedExisting.VehicleGroupId);
        Assert.AreEqual(companyId, capturedExisting.CompanyId);

        Assert.AreEqual(command.DailyPlanDailyRate, capturedExisting.DailyPlan.DailyRate);
        Assert.AreEqual(command.DailyPlanPricePerKilometer, capturedExisting.DailyPlan.PricePerKilometer);
        Assert.AreEqual(command.ControlledPlanDailyRate, capturedExisting.ControlledPlan.DailyRate);
        Assert.AreEqual(command.ControlledPlanExtraPricePerKilometer, capturedExisting.ControlledPlan.ExtraPricePerKilometer);
        Assert.AreEqual(command.FreePlanDailyRate, capturedExisting.FreePlan.DailyRate);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.BillingPlanId), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(expectedFormattedName, command.BillingPlanId), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.ExistsForVehicleGroupAsync(command.VehicleGroupId, command.BillingPlanId), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<BillingPlan>(), It.IsAny<BillingPlan>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        UpdateBillingPlanCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = command.VehicleGroupId;

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        BillingPlan existingPlan = CreateBillingPlan(companyId, vehicleGroupId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(command.BillingPlanId))
            .ReturnsAsync(existingPlan);

        string formattedName = NameFormatter.FormatName(command.Name);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsByNameAsync(formattedName, command.BillingPlanId))
            .ReturnsAsync(false);

        _billingPlanRepositoryMock
            .Setup(r => r.ExistsForVehicleGroupAsync(command.VehicleGroupId, command.BillingPlanId))
            .ReturnsAsync(false);

        _billingPlanRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<BillingPlan>(), It.IsAny<BillingPlan>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<UpdatedBillingPlanDTO> result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante a atualização do plano de cobrança")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}