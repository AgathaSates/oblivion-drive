using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
[TestCategory("BillingPlan - GetAllBillingPlanHandler Unit Tests")]
public class GetAllBillingPlanHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryBillingPlan> _billingPlanRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetAllBillingPlanHandler>> _loggerMock = default!;
    private Mock<IValidator<GetAllBillingPlanQuery>> _validatorMock = default!;
    private GetAllBillingPlanHandler _handler = default!;

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
            .Setup(m => m.Map<List<DetailBillingPlanDTO>>(It.IsAny<IReadOnlyCollection<BillingPlan>>()))
            .Returns(new List<DetailBillingPlanDTO>());

        _loggerMock = new Mock<ILogger<GetAllBillingPlanHandler>>();

        _validatorMock = new Mock<IValidator<GetAllBillingPlanQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetAllBillingPlanQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetAllBillingPlanHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _billingPlanRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetAllBillingPlanQuery CreateValidQuery(int? quantity = null)
        => new(quantity);

    private static User CreateCompanyUser(Guid id, Guid? companyId = null)
        => new()
        {
            Id = id,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? id
        };

    private static BillingPlan CreateBillingPlan(Guid companyId, Guid vehicleGroupId, string name)
    {
        var dailyPlan = new DailyBillingPlanConfig(100m, 1.5m);
        var controlledPlan = new ControlledBillingPlanConfig(80m, 2.0m);
        var freePlan = new FreeBillingPlanConfig(200m);

        return new BillingPlan(
            name,
            companyId,
            vehicleGroupId,
            dailyPlan,
            controlledPlan,
            freePlan);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetAllBillingPlanQuery query = CreateValidQuery();

        List<ValidationFailure> validationFailures =
        [
            new(nameof(GetAllBillingPlanQuery.Quantity), "A quantidade deve ser maior que zero.")
        ];

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<BillingPlanResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailBillingPlanDTO>>(It.IsAny<IReadOnlyCollection<BillingPlan>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetAllBillingPlanQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<BillingPlanResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _billingPlanRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailBillingPlanDTO>>(It.IsAny<IReadOnlyCollection<BillingPlan>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Empty_Result_When_No_BillingPlans_And_Quantity_Is_Null()
    {
        // arrange
        GetAllBillingPlanQuery query = CreateValidQuery(quantity: null);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        List<BillingPlan> emptyCollection = new();

        _billingPlanRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(emptyCollection);

        _mapperMock
            .Setup(m => m.Map<List<DetailBillingPlanDTO>>(emptyCollection))
            .Returns(new List<DetailBillingPlanDTO>());

        // act
        Result<BillingPlanResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.IsNotNull(result.Value.BillingPlans);
        Assert.AreEqual(0, result.Value.BillingPlans.Count);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);
        _billingPlanRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_All_BillingPlans_When_Quantity_Is_Null()
    {
        // arrange
        GetAllBillingPlanQuery query = CreateValidQuery(quantity: null);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        List<BillingPlan> billingPlans =
        [
            CreateBillingPlan(companyId, vehicleGroupId, "Plano A"),
            CreateBillingPlan(companyId, vehicleGroupId, "Plano B"),
            CreateBillingPlan(companyId, vehicleGroupId, "Plano C")
        ];

        _billingPlanRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(billingPlans);

        List<DetailBillingPlanDTO> mappedPlans = billingPlans
            .Select(plan => new DetailBillingPlanDTO(
                Id: plan.Id,
                Name: plan.Name,
                VehicleGroupId: plan.VehicleGroupId,
                DailyPlanDailyRate: plan.DailyPlan.DailyRate,
                DailyPlanPricePerKilometer: plan.DailyPlan.PricePerKilometer,
                ControlledPlanDailyRate: plan.ControlledPlan.DailyRate,
                ControlledPlanExtraPricePerKilometer: plan.ControlledPlan.ExtraPricePerKilometer,
                FreePlanDailyRate: plan.FreePlan.DailyRate))
            .ToList();

        _mapperMock
            .Setup(m => m.Map<List<DetailBillingPlanDTO>>(billingPlans))
            .Returns(mappedPlans);

        // act
        Result<BillingPlanResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(mappedPlans.Count, result.Value.BillingPlans.Count);

        var expectedNames = mappedPlans.Select(p => p.Name).ToList();
        var actualNames = result.Value.BillingPlans.Select(p => p.Name).ToList();
        CollectionAssert.AreEquivalent(expectedNames, actualNames);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);
        _billingPlanRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Respect_Quantity_When_Provided()
    {
        // arrange
        int quantity = 2;
        GetAllBillingPlanQuery query = CreateValidQuery(quantity);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        List<BillingPlan> billingPlans =
        [
            CreateBillingPlan(companyId, vehicleGroupId, "Plano 1"),
            CreateBillingPlan(companyId, vehicleGroupId, "Plano 2")
        ];

        _billingPlanRepositoryMock
            .Setup(r => r.GetAllAsync(quantity))
            .ReturnsAsync(billingPlans);

        List<DetailBillingPlanDTO> mappedPlans = billingPlans
            .Select(plan => new DetailBillingPlanDTO(
                Id: plan.Id,
                Name: plan.Name,
                VehicleGroupId: plan.VehicleGroupId,
                DailyPlanDailyRate: plan.DailyPlan.DailyRate,
                DailyPlanPricePerKilometer: plan.DailyPlan.PricePerKilometer,
                ControlledPlanDailyRate: plan.ControlledPlan.DailyRate,
                ControlledPlanExtraPricePerKilometer: plan.ControlledPlan.ExtraPricePerKilometer,
                FreePlanDailyRate: plan.FreePlan.DailyRate))
            .ToList();

        _mapperMock
            .Setup(m => m.Map<List<DetailBillingPlanDTO>>(billingPlans))
            .Returns(mappedPlans);

        // act
        Result<BillingPlanResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(quantity, result.Value.BillingPlans.Count);

        _billingPlanRepositoryMock.Verify(r =>
            r.GetAllAsync(quantity), Times.Once);
        _billingPlanRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_LogError_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        GetAllBillingPlanQuery query = CreateValidQuery();

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
            .Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<BillingPlanResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) =>
                    value.ToString()!.Contains("Ocorreu um erro durante a listagem de planos de cobrança da empresa")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}