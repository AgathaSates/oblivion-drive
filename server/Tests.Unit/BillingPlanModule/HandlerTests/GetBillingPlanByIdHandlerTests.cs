using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.BillingPlanModule.DTOs;
using OblivionDrive.Application.BillingPlanModule.Handlers;
using OblivionDrive.Application.BillingPlanModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.BillingPlanModule;

namespace OblivionDrive.Tests.Unit.BillingPlanModule.HandlerTests;

[TestClass]
[TestCategory("BillingPlan - GetBillingPlanByIdHandler Unit Tests")]
public class GetBillingPlanByIdHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryBillingPlan> _billingPlanRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetBillingPlanByIdHandler>> _loggerMock = default!;
    private Mock<IValidator<GetBillingPlanByIdQuery>> _validatorMock = default!;
    private GetBillingPlanByIdHandler _handler = default!;

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

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<DetailBillingPlanDTO>(It.IsAny<BillingPlan>()))
            .Returns(default(DetailBillingPlanDTO)!);

        _loggerMock = new Mock<ILogger<GetBillingPlanByIdHandler>>();

        _validatorMock = new Mock<IValidator<GetBillingPlanByIdQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetBillingPlanByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetBillingPlanByIdHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _billingPlanRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetBillingPlanByIdQuery CreateValidQuery()
    {
        return new GetBillingPlanByIdQuery(Guid.NewGuid());
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

    private static BillingPlan CreateBillingPlan(Guid companyId, Guid vehicleGroupId, string name = "Plano Teste")
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
        GetBillingPlanByIdQuery query = CreateValidQuery();

        List<ValidationFailure> validationFailures =
        [
            new(nameof(GetBillingPlanByIdQuery.BillingPlanId), "O identificador do plano de cobrança é obrigatório.")
        ];

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<DetailBillingPlanDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailBillingPlanDTO>(It.IsAny<BillingPlan>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetBillingPlanByIdQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<DetailBillingPlanDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailBillingPlanDTO>(It.IsAny<BillingPlan>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetBillingPlanByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<DetailBillingPlanDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailBillingPlanDTO>(It.IsAny<BillingPlan>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_BillingPlan_Does_Not_Exist()
    {
        // arrange
        GetBillingPlanByIdQuery query = CreateValidQuery();

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
            .Setup(r => r.GetByIdAsync(query.BillingPlanId))
            .ReturnsAsync((BillingPlan?)null);

        // act
        Result<DetailBillingPlanDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.BillingPlanId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailBillingPlanDTO>(It.IsAny<BillingPlan>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_BillingPlan_Belongs_To_Other_Company()
    {
        // arrange
        GetBillingPlanByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        BillingPlan planFromOtherCompany = CreateBillingPlan(otherCompanyId, vehicleGroupId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(query.BillingPlanId))
            .ReturnsAsync(planFromOtherCompany);

        // act
        Result<DetailBillingPlanDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.BillingPlanId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailBillingPlanDTO>(It.IsAny<BillingPlan>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_BillingPlanDetail_When_Request_Is_Valid()
    {
        // arrange
        GetBillingPlanByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        BillingPlan existingPlan = CreateBillingPlan(companyId, vehicleGroupId, "Plano Detalhado");

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _billingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(query.BillingPlanId))
            .ReturnsAsync(existingPlan);

        DetailBillingPlanDTO expectedDetail = new(
            Id: existingPlan.Id,
            Name: existingPlan.Name,
            VehicleGroupId: existingPlan.VehicleGroupId,
            DailyPlanDailyRate: existingPlan.DailyPlan.DailyRate,
            DailyPlanPricePerKilometer: existingPlan.DailyPlan.PricePerKilometer,
            ControlledPlanDailyRate: existingPlan.ControlledPlan.DailyRate,
            ControlledPlanExtraPricePerKilometer: existingPlan.ControlledPlan.ExtraPricePerKilometer,
            FreePlanDailyRate: existingPlan.FreePlan.DailyRate
        );

        _mapperMock
            .Setup(m => m.Map<DetailBillingPlanDTO>(existingPlan))
            .Returns(expectedDetail);

        // act
        Result<DetailBillingPlanDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDetail, result.Value);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.BillingPlanId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailBillingPlanDTO>(existingPlan), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_LogError_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        GetBillingPlanByIdQuery query = CreateValidQuery();

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
            .Setup(r => r.GetByIdAsync(query.BillingPlanId))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<DetailBillingPlanDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) =>
                    value.ToString()!.Contains("Ocorreu um erro durante a obtenção de detalhes do plano de cobrança")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}