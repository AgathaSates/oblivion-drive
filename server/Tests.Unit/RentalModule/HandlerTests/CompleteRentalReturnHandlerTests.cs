using System.Runtime.CompilerServices;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.RentalModule.Commands;
using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Application.RentalModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Tests.Unit.RentalModule.HandlerTests;

[TestClass]
[TestCategory("Rental - CompleteRentalReturnHandler Unit Tests")]
public class CompleteRentalReturnHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;
    private Mock<IRepositoryVehicle> _vehicleRepositoryMock = default!;
    private Mock<IRepositoryBillingPlan> _billingPlanRepositoryMock = default!;
    private Mock<IRepositoryServices> _serviceRepositoryMock = default!;
    private Mock<IRepositoryFuelPriceSettings> _fuelPriceSettingsRepositoryMock = default!;
    private Mock<IRepositoryCoupon> _couponRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IValidator<CompleteRentalReturnCommand>> _validatorMock = default!;
    private Mock<ILogger<CompleteRentalReturnHandler>> _loggerMock = default!;

    private Mock<RentalPricingCalculator> _pricingCalculatorMock = default!;

    private CompleteRentalReturnHandler _handler = default!;

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

        _rentalRepositoryMock = new Mock<IRepositoryRental>();
        _vehicleRepositoryMock = new Mock<IRepositoryVehicle>();
        _billingPlanRepositoryMock = new Mock<IRepositoryBillingPlan>();
        _serviceRepositoryMock = new Mock<IRepositoryServices>();
        _fuelPriceSettingsRepositoryMock = new Mock<IRepositoryFuelPriceSettings>();
        _couponRepositoryMock = new Mock<IRepositoryCoupon>();

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<CompleteRentalReturnCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CompleteRentalReturnCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<CompleteRentalReturnHandler>>();

        _pricingCalculatorMock = new Mock<RentalPricingCalculator>();

        _handler = new CompleteRentalReturnHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _rentalRepositoryMock.Object,
            _vehicleRepositoryMock.Object,
            _billingPlanRepositoryMock.Object,
            _serviceRepositoryMock.Object,
            _fuelPriceSettingsRepositoryMock.Object,
            _couponRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _pricingCalculatorMock.Object
        );
    }

    private static CompleteRentalReturnCommand CreateValidCommand(
        string? couponName = null,
        bool isFuelTankFullOnReturn = true,
        bool hasDamage = false,
        DateOnly? actualReturnDate = null)
    {
        return new CompleteRentalReturnCommand(
            RentalId: Guid.NewGuid(),
            ActualReturnDate: actualReturnDate ?? new DateOnly(2025, 1, 12),
            InitialOdometerInKm: 1000,
            CurrentOdometerInKm: 1100,
            IsFuelTankFullOnReturn: isFuelTankFullOnReturn,
            HasDamage: hasDamage,
            CouponName: couponName
        );
    }

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
        => new()
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? userId
        };

    private static Rental CreateRental(Guid companyId, Guid vehicleId, DateOnly startDate, bool isCompleted = false)
    {
        var rental = new Rental(
            companyId: companyId,
            clientId: Guid.NewGuid(),
            driverId: Guid.NewGuid(),
            vehicleId: vehicleId,
            planType: RentalPlanType.Free,
            startDate: startDate,
            expectedReturnDate: new DateOnly(2025, 1, 11),
            insuranceDailyPricePerPerson: 10m,
            insurancePersonsCount: 1,
            estimatedTotalKilometers: 0,
            servicesTotalPrice: 0m,
            insuranceTotalPrice: 0m,
            rentalBasePrice: 0m,
            estimatedRentalAmount: 0m,
            serviceIds: null
        );

        if (isCompleted)
        {
            SetNonPublicProperty(rental, nameof(Rental.IsCompleted), true);
        }

        return rental;
    }

    private static Vehicle CreateVehicle(Guid id, Guid companyId, Guid vehicleGroupId, FuelType fuelType = FuelType.Gasoline)
    {
        Vehicle vehicle = CreateUninitialized<Vehicle>();
        SetNonPublicProperty(vehicle, nameof(Vehicle.Id), id);
        SetNonPublicProperty(vehicle, nameof(Vehicle.CompanyId), companyId);
        SetNonPublicProperty(vehicle, nameof(Vehicle.VehicleGroupId), vehicleGroupId);
        SetNonPublicProperty(vehicle, nameof(Vehicle.FuelType), fuelType);
        return vehicle;
    }

    private static BillingPlan CreateBillingPlan()
        => CreateUninitialized<BillingPlan>();

    private static FuelPriceConfiguration CreateFuelPriceConfiguration()
        => CreateUninitialized<FuelPriceConfiguration>();

    private static T CreateUninitialized<T>() where T : class
        => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static void SetNonPublicProperty<TObj, TValue>(TObj instance, string propertyName, TValue value)
    {
        var propertyInfo = instance!
            .GetType()
            .GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        if (propertyInfo is null)
            throw new InvalidOperationException($"Property '{propertyName}' not found on type '{instance.GetType().Name}'.");

        var setter = propertyInfo.GetSetMethod(true);
        if (setter is null)
            throw new InvalidOperationException($"Property '{propertyName}' on '{instance.GetType().Name}' does not have a setter.");

        setter.Invoke(instance, new object?[] { value });
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand();

        var failures = new List<ValidationFailure>
        {
            new(nameof(CompleteRentalReturnCommand.RentalId), "O identificador do aluguel é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // act
        Result<CompletedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns((Guid?)null);

        // act
        Result<CompletedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<CompletedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(currentUserId.ToString()), Times.Once);
        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Rental_Does_Not_Exist()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(command.RentalId)).ReturnsAsync((Rental?)null);

        // act
        Result<CompletedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(command.RentalId), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Rental_Belongs_To_Other_Company()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Guid vehicleId = Guid.NewGuid();
        Rental rentalFromOtherCompany = CreateRental(otherCompanyId, vehicleId, startDate: new DateOnly(2025, 1, 10));

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(command.RentalId)).ReturnsAsync(rentalFromOtherCompany);

        // act
        Result<CompletedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleRepositoryMock.Verify(v => v.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Rental_Is_Already_Completed()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Guid vehicleId = Guid.NewGuid();
        Rental completedRental = CreateRental(companyId, vehicleId, startDate: new DateOnly(2025, 1, 10), isCompleted: true);

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);
        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(command.RentalId)).ReturnsAsync(completedRental);

        // act
        Result<CompletedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleRepositoryMock.Verify(v => v.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_ActualReturnDate_Is_Before_StartDate()
    {
        // arrange
        DateOnly startDate = new(2025, 1, 10);
        CompleteRentalReturnCommand command = CreateValidCommand(actualReturnDate: new DateOnly(2025, 1, 9));

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Guid vehicleId = Guid.NewGuid();
        Rental rental = CreateRental(companyId, vehicleId, startDate: startDate);

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);
        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(command.RentalId)).ReturnsAsync(rental);

        // act
        Result<CompletedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleRepositoryMock.Verify(v => v.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_FuelPriceConfiguration_Is_Missing()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Guid vehicleId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        Rental rental = CreateRental(companyId, vehicleId, startDate: new DateOnly(2025, 1, 10));
        Vehicle vehicle = CreateVehicle(vehicleId, companyId, vehicleGroupId);
        BillingPlan billingPlan = CreateBillingPlan();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(command.RentalId)).ReturnsAsync(rental);
        _vehicleRepositoryMock.Setup(v => v.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);
        _billingPlanRepositoryMock.Setup(b => b.GetByVehicleGroupIdAsync(vehicleGroupId)).ReturnsAsync(billingPlan);

        _fuelPriceSettingsRepositoryMock.Setup(f => f.GetAsync(companyId)).ReturnsAsync((FuelPriceConfiguration?)null);

        // act
        Result<CompletedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _couponRepositoryMock.Verify(c => c.GetByNameAsync(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Coupon_Does_Not_Exist()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand(couponName: "  cupom10  ");

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Guid vehicleId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        Rental rental = CreateRental(companyId, vehicleId, startDate: new DateOnly(2025, 1, 10));
        Vehicle vehicle = CreateVehicle(vehicleId, companyId, vehicleGroupId);
        BillingPlan billingPlan = CreateBillingPlan();
        FuelPriceConfiguration fuelConfig = CreateFuelPriceConfiguration();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(command.RentalId)).ReturnsAsync(rental);
        _vehicleRepositoryMock.Setup(v => v.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);
        _billingPlanRepositoryMock.Setup(b => b.GetByVehicleGroupIdAsync(vehicleGroupId)).ReturnsAsync(billingPlan);
        _fuelPriceSettingsRepositoryMock.Setup(f => f.GetAsync(companyId)).ReturnsAsync(fuelConfig);

        _pricingCalculatorMock.Setup(p => p.CalculateRentalDays(It.IsAny<DateOnly>(), It.IsAny<DateOnly>())).Returns(2);
        _pricingCalculatorMock.Setup(p => p.CalculateFinalRentalAmountOnReturn(
                It.IsAny<BillingPlan>(), It.IsAny<RentalPlanType>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
            .Returns(100m);
        _pricingCalculatorMock.Setup(p => p.CalculateInsuranceTotalPrice(It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(20m);
        _pricingCalculatorMock.Setup(p => p.CalculateServicesTotalPrice(It.IsAny<IReadOnlyCollection<Service>>(), It.IsAny<int>()))
            .Returns(0m);
        _pricingCalculatorMock.Setup(p => p.CalculateLateReturnPenalty(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<decimal>()))
            .Returns(0m);
        _pricingCalculatorMock.Setup(p => p.CalculateOnReturn(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
            .Returns(120m);

        _couponRepositoryMock.Setup(c => c.GetByNameAsync("CUPOM10")).ReturnsAsync((Coupon?)null);

        // act
        Result<CompletedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _couponRepositoryMock.Verify(c => c.GetByNameAsync("CUPOM10"), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Complete_Return_And_Return_Success_When_Request_Is_Valid()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand(isFuelTankFullOnReturn: true, hasDamage: false);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Guid vehicleId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        Rental rental = CreateRental(companyId, vehicleId, startDate: new DateOnly(2025, 1, 10));
        Vehicle vehicle = CreateVehicle(vehicleId, companyId, vehicleGroupId);
        BillingPlan billingPlan = CreateBillingPlan();
        FuelPriceConfiguration fuelConfig = CreateFuelPriceConfiguration();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(command.RentalId)).ReturnsAsync(rental);
        _vehicleRepositoryMock.Setup(v => v.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);
        _billingPlanRepositoryMock.Setup(b => b.GetByVehicleGroupIdAsync(vehicleGroupId)).ReturnsAsync(billingPlan);
        _fuelPriceSettingsRepositoryMock.Setup(f => f.GetAsync(companyId)).ReturnsAsync(fuelConfig);

        _pricingCalculatorMock.Setup(p => p.CalculateRentalDays(It.IsAny<DateOnly>(), It.IsAny<DateOnly>())).Returns(2);
        _pricingCalculatorMock.Setup(p => p.CalculateFinalRentalAmountOnReturn(
                It.IsAny<BillingPlan>(), It.IsAny<RentalPlanType>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
            .Returns(100m);
        _pricingCalculatorMock.Setup(p => p.CalculateInsuranceTotalPrice(It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(20m);
        _pricingCalculatorMock.Setup(p => p.CalculateServicesTotalPrice(It.IsAny<IReadOnlyCollection<Service>>(), It.IsAny<int>()))
            .Returns(0m);
        _pricingCalculatorMock.Setup(p => p.CalculateLateReturnPenalty(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<decimal>()))
            .Returns(0m);
        _pricingCalculatorMock.Setup(p => p.CalculateOnReturn(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
            .Returns(120m);

        // act
        Result<CompletedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);

        Assert.IsTrue(result.Value.CompletedSuccessfully);
        Assert.AreEqual(rental.Id, result.Value.RentalId);

        Assert.AreEqual(120m, result.Value.GrossRentalAmount);
        Assert.AreEqual(0m, result.Value.FinalAmountToPay);

        Assert.IsTrue(rental.IsCompleted);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }
}