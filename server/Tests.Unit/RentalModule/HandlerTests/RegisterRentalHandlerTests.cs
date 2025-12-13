using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using AutoMapper;
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
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Tests.Unit.RentalModule.HandlerTests;

[TestClass]
[TestCategory("Rental - RegisterRentalHandler Unit Tests")]
public class RegisterRentalHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;
    private Mock<IRepositoryClient> _clientRepositoryMock = default!;
    private Mock<IRepositoryDriver> _driverRepositoryMock = default!;
    private Mock<IRepositoryVehicle> _vehicleRepositoryMock = default!;
    private Mock<IRepositoryBillingPlan> _billingPlanRepositoryMock = default!;
    private Mock<IRepositoryServices> _serviceRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IValidator<RegisterRentalCommand>> _validatorMock = default!;
    private Mock<ILogger<RegisterRentalCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;

    private RentalPricingCalculator _pricingCalculator = default!;
    private RegisterRentalHandler _handler = default!;

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
        _clientRepositoryMock = new Mock<IRepositoryClient>();
        _driverRepositoryMock = new Mock<IRepositoryDriver>();
        _vehicleRepositoryMock = new Mock<IRepositoryVehicle>();
        _billingPlanRepositoryMock = new Mock<IRepositoryBillingPlan>();
        _serviceRepositoryMock = new Mock<IRepositoryServices>();

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<RegisterRentalCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterRentalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<RegisterRentalCommand>>();

        _mapperMock = new Mock<IMapper>();

        _pricingCalculator = new RentalPricingCalculator();

        _handler = new RegisterRentalHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _rentalRepositoryMock.Object,
            _clientRepositoryMock.Object,
            _driverRepositoryMock.Object,
            _vehicleRepositoryMock.Object,
            _billingPlanRepositoryMock.Object,
            _serviceRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _mapperMock.Object,
            _pricingCalculator
        );
    }

    private static RegisterRentalCommand CreateValidCommand(
        Guid clientId,
        Guid driverId,
        Guid vehicleId,
        RentalPlanType planType = RentalPlanType.Daily,
        DateOnly? startDate = null,
        DateOnly? expectedReturnDate = null,
        IReadOnlyCollection<Guid>? serviceIds = null,
        int? estimatedKm = null)
    {
        DateOnly start = startDate ?? new DateOnly(2025, 1, 10);
        DateOnly end = expectedReturnDate ?? new DateOnly(2025, 1, 12);

        return new RegisterRentalCommand(
            ClientId: clientId,
            DriverId: driverId,
            VehicleId: vehicleId,
            PlanType: planType,
            StartDate: start,
            ExpectedReturnDate: end,
            InsuranceDailyPricePerPerson: 10m,
            InsurancePersonsCount: 2,
            EstimatedTotalKilometers: estimatedKm,
            ServiceIds: serviceIds
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

    private static T CreateUninitialized<T>() where T : class
    => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static void SetProp<T>(T instance, string propertyName, object? value) where T : class
    {
        PropertyInfo? prop = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop is null)
            throw new InvalidOperationException($"Propriedade '{typeof(T).Name}.{propertyName}' não encontrada.");

        MethodInfo? setter = prop.GetSetMethod(true);
        if (setter is null)
            throw new InvalidOperationException($"Setter de '{typeof(T).Name}.{propertyName}' não encontrado (não tem set?).");

        setter.Invoke(instance, new[] { value });
    }

    private static Client CreateClient(Guid id, Guid companyId, ClientType clientType)
    {
        Client client = CreateUninitialized<Client>();
        SetProp(client, nameof(Client.Id), id);
        SetProp(client, nameof(Client.CompanyId), companyId);
        SetProp(client, nameof(Client.ClientType), clientType);
        return client;
    }

    private static Driver CreateDriver(
        Guid id,
        Guid companyId,
        Guid clientId,
        bool isClientAlsoDriver,
        DateOnly cnhExpirationDate)
    {
        Driver driver = CreateUninitialized<Driver>();
        SetProp(driver, nameof(Driver.Id), id);
        SetProp(driver, nameof(Driver.CompanyId), companyId);
        SetProp(driver, nameof(Driver.ClientId), clientId);
        SetProp(driver, nameof(Driver.IsClientAlsoDriver), isClientAlsoDriver);
        SetProp(driver, nameof(Driver.CnhExpirationDate), cnhExpirationDate);
        return driver;
    }

    private static Vehicle CreateVehicle(Guid id, Guid companyId, Guid vehicleGroupId)
    {
        Vehicle vehicle = CreateUninitialized<Vehicle>();
        SetProp(vehicle, nameof(Vehicle.Id), id);
        SetProp(vehicle, nameof(Vehicle.CompanyId), companyId);
        SetProp(vehicle, nameof(Vehicle.VehicleGroupId), vehicleGroupId);
        return vehicle;
    }

    private static BillingPlan CreateBillingPlan(
        decimal dailyRate = 100m,
        decimal pricePerKm = 2m,
        decimal controlledDailyRate = 120m,
        decimal extraPricePerKm = 3m,
        decimal freeDailyRate = 80m)
    {
        BillingPlan billingPlan = CreateUninitialized<BillingPlan>();

        object dailyPlan = CreatePlanObject(billingPlan, "DailyPlan",
            ("DailyRate", dailyRate),
            ("PricePerKilometer", pricePerKm));

        object controlledPlan = CreatePlanObject(billingPlan, "ControlledPlan",
            ("DailyRate", controlledDailyRate),
            ("ExtraPricePerKilometer", extraPricePerKm));

        object freePlan = CreatePlanObject(billingPlan, "FreePlan",
            ("DailyRate", freeDailyRate));

        SetProp(billingPlan, "DailyPlan", dailyPlan);
        SetProp(billingPlan, "ControlledPlan", controlledPlan);
        SetProp(billingPlan, "FreePlan", freePlan);

        return billingPlan;
    }

    private static object CreatePlanObject(object owner, string planPropertyName, params (string prop, object value)[] values)
    {
        PropertyInfo? planProp = owner.GetType().GetProperty(planPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (planProp is null)
            throw new InvalidOperationException($"Propriedade '{owner.GetType().Name}.{planPropertyName}' não encontrada.");

        object plan = RuntimeHelpers.GetUninitializedObject(planProp.PropertyType);

        foreach ((string prop, object value) in values)
        {
            PropertyInfo? p = planProp.PropertyType.GetProperty(prop, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p is null)
                throw new InvalidOperationException($"Propriedade '{planProp.PropertyType.Name}.{prop}' não encontrada.");

            MethodInfo? set = p.GetSetMethod(true);
            if (set is null)
                throw new InvalidOperationException($"Setter de '{planProp.PropertyType.Name}.{prop}' não encontrado.");

            set.Invoke(plan, new[] { value });
        }

        return plan;
    }

    private static Service CreateService(Guid id, Guid companyId, decimal price = 10m, ChargeType chargeType = ChargeType.Fixed)
    {
        Service service = CreateUninitialized<Service>();
        SetProp(service, nameof(Service.Id), id);
        SetProp(service, nameof(Service.CompanyId), companyId);
        SetProp(service, nameof(Service.Price), price);
        SetProp(service, nameof(Service.ChargeType), chargeType);
        return service;
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(RegisterRentalCommand.ClientId), "O identificador do cliente é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<RentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _clientRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _driverRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _rentalRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Rental>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<RentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _clientRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<RentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _clientRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Client_Does_Not_Exist()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync((Client?)null);

        // act
        Result<RentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _clientRepositoryMock.Verify(r => r.GetByIdAsync(command.ClientId), Times.Once);

        _driverRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _rentalRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Rental>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Client_Belongs_To_Other_Company()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Client clientFromOtherCompany = CreateClient(command.ClientId, otherCompanyId, ClientType.LegalEntity);

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(clientFromOtherCompany);

        // act
        Result<RentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _driverRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _rentalRepositoryMock.Verify(r => r.ExistsOpenRentalForVehicleAsync(It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Driver_Cnh_Is_Expired_For_StartDate()
    {
        // arrange
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();

        RegisterRentalCommand command = CreateValidCommand(clientId, driverId, vehicleId, startDate: new DateOnly(2025, 1, 10));

        Client client = CreateClient(clientId, companyId, ClientType.LegalEntity);

        Driver driver = CreateDriver(driverId, companyId, clientId, isClientAlsoDriver: false, cnhExpirationDate: new DateOnly(2024, 12, 31));

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        _driverRepositoryMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);

        // act
        Result<RentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.ExistsOpenRentalForVehicleAsync(It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Vehicle_Has_Open_Rental()
    {
        // arrange
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        RegisterRentalCommand command = CreateValidCommand(clientId, driverId, vehicleId);

        Client client = CreateClient(clientId, companyId, ClientType.LegalEntity);
        Driver driver = CreateDriver(driverId, companyId, clientId, isClientAlsoDriver: false, cnhExpirationDate: new DateOnly(2030, 1, 1));
        Vehicle vehicle = CreateVehicle(vehicleId, companyId, vehicleGroupId);

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        _driverRepositoryMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        _vehicleRepositoryMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);

        _rentalRepositoryMock
            .Setup(r => r.ExistsOpenRentalForVehicleAsync(vehicleId))
            .ReturnsAsync(true);

        // act
        Result<RentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _billingPlanRepositoryMock.Verify(r => r.GetByVehicleGroupIdAsync(It.IsAny<Guid>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Rental>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_BillingPlan_Does_Not_Exist()
    {
        // arrange
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        RegisterRentalCommand command = CreateValidCommand(clientId, driverId, vehicleId);

        Client client = CreateClient(clientId, companyId, ClientType.LegalEntity);
        Driver driver = CreateDriver(driverId, companyId, clientId, isClientAlsoDriver: false, cnhExpirationDate: new DateOnly(2030, 1, 1));
        Vehicle vehicle = CreateVehicle(vehicleId, companyId, vehicleGroupId);

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        _driverRepositoryMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        _vehicleRepositoryMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);

        _rentalRepositoryMock.Setup(r => r.ExistsOpenRentalForVehicleAsync(vehicleId)).ReturnsAsync(false);

        _billingPlanRepositoryMock
            .Setup(r => r.GetByVehicleGroupIdAsync(vehicleGroupId))
            .ReturnsAsync((BillingPlan?)null);

        // act
        Result<RentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Rental>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_When_Selected_Service_Does_Not_Exist()
    {
        // arrange
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        Guid serviceId = Guid.NewGuid();
        IReadOnlyCollection<Guid> serviceIds = new[] { serviceId };

        RegisterRentalCommand command = CreateValidCommand(clientId, driverId, vehicleId, serviceIds: serviceIds);

        Client client = CreateClient(clientId, companyId, ClientType.LegalEntity);
        Driver driver = CreateDriver(driverId, companyId, clientId, isClientAlsoDriver: false, cnhExpirationDate: new DateOnly(2030, 1, 1));
        Vehicle vehicle = CreateVehicle(vehicleId, companyId, vehicleGroupId);

        BillingPlan billingPlan = CreateBillingPlan();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        _driverRepositoryMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        _vehicleRepositoryMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);

        _rentalRepositoryMock.Setup(r => r.ExistsOpenRentalForVehicleAsync(vehicleId)).ReturnsAsync(false);
        _billingPlanRepositoryMock.Setup(r => r.GetByVehicleGroupIdAsync(vehicleGroupId)).ReturnsAsync(billingPlan);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(serviceId))
            .ReturnsAsync((Service?)null);

        // act
        Result<RentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Rental>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Create_Rental_And_Return_Success_When_Request_Is_Valid()
    {
        // arrange
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        RegisterRentalCommand command = CreateValidCommand(clientId, driverId, vehicleId);

        Client client = CreateClient(clientId, companyId, ClientType.LegalEntity);
        Driver driver = CreateDriver(driverId, companyId, clientId, isClientAlsoDriver: false, cnhExpirationDate: new DateOnly(2030, 1, 1));
        Vehicle vehicle = CreateVehicle(vehicleId, companyId, vehicleGroupId);

        BillingPlan billingPlan = CreateBillingPlan(dailyRate: 100m, pricePerKm: 2m);

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        _driverRepositoryMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        _vehicleRepositoryMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);

        _rentalRepositoryMock.Setup(r => r.ExistsOpenRentalForVehicleAsync(vehicleId)).ReturnsAsync(false);
        _billingPlanRepositoryMock.Setup(r => r.GetByVehicleGroupIdAsync(vehicleGroupId)).ReturnsAsync(billingPlan);

        Rental? capturedRental = null;

        _rentalRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rental>()))
            .Callback<Rental>(r => capturedRental = r)
            .ReturnsAsync(Guid.NewGuid());

        RentalDTO expectedDto = CreateUninitialized<RentalDTO>();

        _mapperMock
            .Setup(m => m.Map<RentalDTO>(It.IsAny<Rental>()))
            .Returns(expectedDto);

        // act
        Result<RentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreSame(expectedDto, result.Value);

        Assert.IsNotNull(capturedRental);
        Assert.AreNotEqual(Guid.Empty, capturedRental!.Id);
        Assert.AreEqual(companyId, capturedRental.CompanyId);
        Assert.AreEqual(clientId, capturedRental.ClientId);
        Assert.AreEqual(driverId, capturedRental.DriverId);
        Assert.AreEqual(vehicleId, capturedRental.VehicleId);
        Assert.AreEqual(command.PlanType, capturedRental.PlanType);

        _rentalRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Rental>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        _serviceRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        RegisterRentalCommand command = CreateValidCommand(clientId, driverId, vehicleId);

        Client client = CreateClient(clientId, companyId, ClientType.LegalEntity);
        Driver driver = CreateDriver(driverId, companyId, clientId, isClientAlsoDriver: false, cnhExpirationDate: new DateOnly(2030, 1, 1));
        Vehicle vehicle = CreateVehicle(vehicleId, companyId, vehicleGroupId);

        BillingPlan billingPlan = CreateBillingPlan();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        _driverRepositoryMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        _vehicleRepositoryMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);

        _rentalRepositoryMock.Setup(r => r.ExistsOpenRentalForVehicleAsync(vehicleId)).ReturnsAsync(false);
        _billingPlanRepositoryMock.Setup(r => r.GetByVehicleGroupIdAsync(vehicleGroupId)).ReturnsAsync(billingPlan);

        _rentalRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rental>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<RentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante o registro de aluguel")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}