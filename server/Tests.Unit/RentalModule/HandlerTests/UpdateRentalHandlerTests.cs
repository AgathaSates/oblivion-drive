using System.Reflection;
using System.Runtime.CompilerServices;
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
[TestCategory("Rental - UpdateRentalHandler Unit Tests")]
public class UpdateRentalHandlerTests
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
    private Mock<IValidator<UpdateRentalCommand>> _validatorMock = default!;
    private Mock<ILogger<UpdateRentalCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;

    private RentalPricingCalculator _rentalPricingCalculator = default!;
    private UpdateRentalHandler _handler = default!;

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
        _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<UpdateRentalCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateRentalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<UpdateRentalCommand>>();
        _mapperMock = new Mock<IMapper>();

        _rentalPricingCalculator = new RentalPricingCalculator();

        _handler = new UpdateRentalHandler(
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
            _rentalPricingCalculator
        );
    }

    private static UpdateRentalCommand CreateValidCommand(
        Guid rentalId,
        Guid clientId,
        Guid driverId,
        Guid vehicleId,
        RentalPlanType planType = RentalPlanType.Free,
        DateOnly? startDate = null,
        DateOnly? expectedReturnDate = null,
        IReadOnlyCollection<Guid>? serviceIds = null)
    {
        DateOnly rentalStartDate = startDate ?? new DateOnly(2025, 1, 10);
        DateOnly rentalExpectedReturnDate = expectedReturnDate ?? new DateOnly(2025, 1, 12);

        return new UpdateRentalCommand(
            RentalId: rentalId,
            ClientId: clientId,
            DriverId: driverId,
            VehicleId: vehicleId,
            PlanType: planType,
            StartDate: rentalStartDate,
            ExpectedReturnDate: rentalExpectedReturnDate,
            InsuranceDailyPricePerPerson: 10m,
            InsurancePersonsCount: 1,
            EstimatedTotalKilometers: null,
            ServiceIds: serviceIds ?? Array.Empty<Guid>()
        );
    }

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
    {
        return new User
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? userId
        };
    }

    private static Rental CreateExistingRental(Guid companyId, Guid clientId, Guid driverId, Guid vehicleId)
    {
        return new Rental(
            companyId: companyId,
            clientId: clientId,
            driverId: driverId,
            vehicleId: vehicleId,
            planType: RentalPlanType.Free,
            startDate: new DateOnly(2025, 1, 10),
            expectedReturnDate: new DateOnly(2025, 1, 12),
            insuranceDailyPricePerPerson: 10m,
            insurancePersonsCount: 1,
            estimatedTotalKilometers: 0,
            servicesTotalPrice: 0m,
            insuranceTotalPrice: 0m,
            rentalBasePrice: 0m,
            estimatedRentalAmount: 0m,
            serviceIds: Array.Empty<Guid>()
        );
    }

    private static void AssertFailedWithMessageContains<T>(Result<T> result, string expectedMessagePart)
    {
        Assert.IsTrue(result.IsFailed);

        var allMessages = result.Errors
            .SelectMany(error => new[] { error.Message }.Concat(error.Reasons.Select(r => r.Message)))
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToList();

        Assert.IsTrue(
            allMessages.Any(message => message.Contains(expectedMessagePart, StringComparison.CurrentCulture)),
            $"Era esperado conter '{expectedMessagePart}'. Mensagens: {string.Join(" | ", allMessages)}"
        );
    }

    private static T CreateUninitialized<T>() where T : class
        => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static PropertyInfo GetPropertyInHierarchy(Type type, string propertyName)
    {
        for (Type? currentType = type; currentType is not null; currentType = currentType.BaseType)
        {
            PropertyInfo? property = currentType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is not null)
                return property;
        }

        throw new InvalidOperationException($"Property '{propertyName}' not found on type '{type.FullName}'.");
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        PropertyInfo property = GetPropertyInHierarchy(target.GetType(), propertyName);
        property.SetValue(target, value);
    }

    private static Client CreateClient(Guid clientId, Guid companyId, ClientType clientType)
    {
        Client client = CreateUninitialized<Client>();

        SetProperty(client, "Id", clientId);
        SetProperty(client, "CompanyId", companyId);
        SetProperty(client, "ClientType", clientType);

        return client;
    }

    private static Driver CreateDriver(Guid driverId, Guid companyId, Guid clientId, DateOnly cnhExpirationDate, bool isClientAlsoDriver)
    {
        Driver driver = CreateUninitialized<Driver>();

        SetProperty(driver, "Id", driverId);
        SetProperty(driver, "CompanyId", companyId);
        SetProperty(driver, "ClientId", clientId);
        SetProperty(driver, "CnhExpirationDate", cnhExpirationDate);
        SetProperty(driver, "IsClientAlsoDriver", isClientAlsoDriver);

        return driver;
    }

    private static Vehicle CreateVehicle(Guid vehicleId, Guid companyId, Guid vehicleGroupId)
    {
        Vehicle vehicle = CreateUninitialized<Vehicle>();

        SetProperty(vehicle, "Id", vehicleId);
        SetProperty(vehicle, "CompanyId", companyId);
        SetProperty(vehicle, "VehicleGroupId", vehicleGroupId);

        return vehicle;
    }

    private static BillingPlan CreateBillingPlanWithRates(
        decimal dailyRate = 100m,
        decimal controlledRate = 120m,
        decimal freeRate = 80m,
        decimal dailyPricePerKm = 2m,
        decimal controlledExtraPricePerKm = 3m)
    {
        BillingPlan billingPlan = CreateUninitialized<BillingPlan>();

        // DailyPlan
        PropertyInfo dailyPlanProperty = GetPropertyInHierarchy(typeof(BillingPlan), "DailyPlan");
        object dailyPlan = RuntimeHelpers.GetUninitializedObject(dailyPlanProperty.PropertyType);
        SetProperty(dailyPlan, "DailyRate", dailyRate);
        SetProperty(dailyPlan, "PricePerKilometer", dailyPricePerKm);
        dailyPlanProperty.SetValue(billingPlan, dailyPlan);

        // ControlledPlan
        PropertyInfo controlledPlanProperty = GetPropertyInHierarchy(typeof(BillingPlan), "ControlledPlan");
        object controlledPlan = RuntimeHelpers.GetUninitializedObject(controlledPlanProperty.PropertyType);
        SetProperty(controlledPlan, "DailyRate", controlledRate);
        SetProperty(controlledPlan, "ExtraPricePerKilometer", controlledExtraPricePerKm);
        controlledPlanProperty.SetValue(billingPlan, controlledPlan);

        // FreePlan
        PropertyInfo freePlanProperty = GetPropertyInHierarchy(typeof(BillingPlan), "FreePlan");
        object freePlan = RuntimeHelpers.GetUninitializedObject(freePlanProperty.PropertyType);
        SetProperty(freePlan, "DailyRate", freeRate);
        freePlanProperty.SetValue(billingPlan, freePlan);

        return billingPlan;
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        Guid rentalId = Guid.NewGuid();
        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();

        UpdateRentalCommand command = CreateValidCommand(rentalId, clientId, driverId, vehicleId);

        var failures = new List<ValidationFailure>
        {
            new(nameof(UpdateRentalCommand.RentalId), "O identificador do aluguel é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // act
        Result<UpdatedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

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
        _tenantProviderMock.Setup(tp => tp.UserId).Returns((Guid?)null);

        UpdateRentalCommand command = CreateValidCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // act
        Result<UpdatedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        AssertFailedWithMessageContains(result, "Usuário não está autenticado.");

        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        UpdateRentalCommand command = CreateValidCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // act
        Result<UpdatedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        AssertFailedWithMessageContains(result, "Usuário autenticado não foi encontrado.");

        _userManagerMock.Verify(m => m.FindByIdAsync(currentUserId.ToString()), Times.Once);
        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Rental_Does_Not_Exist()
    {
        // arrange
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        Guid rentalId = Guid.NewGuid();
        UpdateRentalCommand command = CreateValidCommand(rentalId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(rentalId)).ReturnsAsync((Rental?)null);

        // act
        Result<UpdatedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(rentalId), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Rental_Is_Completed()
    {
        // arrange
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        Guid rentalId = Guid.NewGuid();
        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();

        Rental existingRental = CreateExistingRental(companyId, clientId, driverId, vehicleId);
        SetProperty(existingRental, "IsCompleted", true);

        UpdateRentalCommand command = CreateValidCommand(rentalId: existingRental.Id, clientId, driverId, vehicleId);

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(existingRental.Id)).ReturnsAsync(existingRental);

        // act
        Result<UpdatedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        AssertFailedWithMessageContains(result, "Não é possível editar um aluguel já concluído.");

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Update_Rental_And_Return_Success_When_Request_Is_Valid()
    {
        // arrange
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        Rental existingRental = CreateExistingRental(companyId, clientId, driverId, vehicleId);

        UpdateRentalCommand command = CreateValidCommand(
            rentalId: existingRental.Id,
            clientId: clientId,
            driverId: driverId,
            vehicleId: vehicleId,
            planType: RentalPlanType.Free,
            serviceIds: Array.Empty<Guid>());

        Client client = CreateClient(clientId, companyId, ClientType.Individual);
        Driver driver = CreateDriver(
            driverId,
            companyId,
            clientId,
            cnhExpirationDate: command.StartDate.AddDays(10),
            isClientAlsoDriver: false);

        Vehicle vehicle = CreateVehicle(vehicleId, companyId, vehicleGroupId);
        BillingPlan billingPlan = CreateBillingPlanWithRates(freeRate: 50m);

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(existingRental.Id)).ReturnsAsync(existingRental);
        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        _driverRepositoryMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        _vehicleRepositoryMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);
        _billingPlanRepositoryMock.Setup(r => r.GetByVehicleGroupIdAsync(vehicleGroupId)).ReturnsAsync(billingPlan);

        Rental? updatedRentalReturned = null;

        _rentalRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Rental>(), It.IsAny<Rental>()))
            .Callback<Rental, Rental>((existing, updatedData) =>
            {
                existing.Update(updatedData);
                updatedRentalReturned = existing;
            })
            .ReturnsAsync(() => updatedRentalReturned!);

        UpdatedRentalDTO expectedDto =
            (UpdatedRentalDTO)RuntimeHelpers.GetUninitializedObject(typeof(UpdatedRentalDTO));

        _mapperMock
            .Setup(m => m.Map<UpdatedRentalDTO>(It.IsAny<Rental>()))
            .Returns(expectedDto);

        // act
        Result<UpdatedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreSame(expectedDto, result.Value);

        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(existingRental.Id), Times.Once);
        _rentalRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Rental>(), It.IsAny<Rental>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        _mapperMock.Verify(m => m.Map<UpdatedRentalDTO>(It.IsAny<Rental>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(companyUser);

        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        Rental existingRental = CreateExistingRental(companyId, clientId, driverId, vehicleId);

        UpdateRentalCommand command = CreateValidCommand(existingRental.Id, clientId, driverId, vehicleId);

        Client client = CreateClient(clientId, companyId, ClientType.Individual);
        Driver driver = CreateDriver(driverId, companyId, clientId, command.StartDate.AddDays(5), isClientAlsoDriver: false);
        Vehicle vehicle = CreateVehicle(vehicleId, companyId, vehicleGroupId);
        BillingPlan billingPlan = CreateBillingPlanWithRates(freeRate: 50m);

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(existingRental.Id)).ReturnsAsync(existingRental);
        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        _driverRepositoryMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        _vehicleRepositoryMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);
        _billingPlanRepositoryMock.Setup(r => r.GetByVehicleGroupIdAsync(vehicleGroupId)).ReturnsAsync(billingPlan);

        _rentalRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Rental>(), It.IsAny<Rental>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<UpdatedRentalDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a atualização de aluguel")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}