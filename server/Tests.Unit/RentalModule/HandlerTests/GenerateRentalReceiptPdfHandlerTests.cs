using System.Reflection;
using System.Runtime.CompilerServices;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Application.RentalModule.Handlers;
using OblivionDrive.Application.RentalModule.Querys;
using OblivionDrive.Application.RentalModule.Results;
using OblivionDrive.Application.RentalModule.Services;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Tests.Unit.RentalModule.HandlerTests;

[TestClass]
[TestCategory("Rental - GenerateRentalReceiptPdfHandler Unit Tests")]
public class GenerateRentalReceiptPdfHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;
    private Mock<IRepositoryClient> _clientRepositoryMock = default!;
    private Mock<IRepositoryDriver> _driverRepositoryMock = default!;
    private Mock<IRepositoryVehicle> _vehicleRepositoryMock = default!;
    private Mock<IRentalReceiptPdfGenerator> _receiptPdfGeneratorMock = default!;
    private Mock<ILogger<GenerateRentalReceiptPdfHandler>> _loggerMock = default!;

    private GenerateRentalReceiptPdfHandler _handler = default!;

    [TestInitialize]
    public void Setup()
    {
        var userStoreMock = new Mock<IUserStore<User>>();
        var identityOptions = Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<User>>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorDescriber = new IdentityErrorDescriber();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var loggerUserManagerMock = new Mock<ILogger<UserManager<User>>>();

        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object,
            identityOptions,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            keyNormalizerMock.Object,
            errorDescriber,
            serviceProviderMock.Object,
            loggerUserManagerMock.Object
        );

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock.Setup(tp => tp.UserId).Returns(Guid.NewGuid());

        _rentalRepositoryMock = new Mock<IRepositoryRental>();
        _clientRepositoryMock = new Mock<IRepositoryClient>();
        _driverRepositoryMock = new Mock<IRepositoryDriver>();
        _vehicleRepositoryMock = new Mock<IRepositoryVehicle>();

        _receiptPdfGeneratorMock = new Mock<IRentalReceiptPdfGenerator>();
        _receiptPdfGeneratorMock.Setup(g => g.Generate(It.IsAny<RentalReceiptPdfData>())).Returns(Array.Empty<byte>());

        _loggerMock = new Mock<ILogger<GenerateRentalReceiptPdfHandler>>();

        _handler = new GenerateRentalReceiptPdfHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _rentalRepositoryMock.Object,
            _clientRepositoryMock.Object,
            _driverRepositoryMock.Object,
            _vehicleRepositoryMock.Object,
            _receiptPdfGeneratorMock.Object,
            _loggerMock.Object
        );
    }

    private static GenerateRentalReceiptPdfQuery CreateValidQuery()
        => new(RentalId: Guid.NewGuid());

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
        => new()
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? userId
        };

    private static Rental CreateRental(Guid companyId, Guid clientId, Guid driverId, Guid vehicleId, RentalPlanType planType = RentalPlanType.Daily)
        => new(
            companyId: companyId,
            clientId: clientId,
            driverId: driverId,
            vehicleId: vehicleId,
            planType: planType,
            startDate: new DateOnly(2025, 1, 1),
            expectedReturnDate: new DateOnly(2025, 1, 2),
            insuranceDailyPricePerPerson: 10m,
            insurancePersonsCount: 1,
            estimatedTotalKilometers: 0,
            servicesTotalPrice: 0m,
            insuranceTotalPrice: 0m,
            rentalBasePrice: 100m,
            estimatedRentalAmount: 100m,
            serviceIds: null
        );

    private static T CreateUninitialized<T>() where T : class
        => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static void SetNonPublicProperty<TObj, TValue>(TObj instance, string propertyName, TValue value)
    {
        PropertyInfo? propertyInfo = instance!
            .GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (propertyInfo is null)
            throw new InvalidOperationException($"Property '{propertyName}' not found on type '{instance!.GetType().Name}'.");

        MethodInfo? setter = propertyInfo.GetSetMethod(true);
        if (setter is null)
            throw new InvalidOperationException($"Property '{propertyName}' on '{instance!.GetType().Name}' does not have a setter.");

        setter.Invoke(instance, new object?[] { value });
    }

    private static Client CreateClient(Guid id, Guid companyId, string name)
    {
        Client client = CreateUninitialized<Client>();
        SetNonPublicProperty(client, nameof(Client.Id), id);
        SetNonPublicProperty(client, nameof(Client.CompanyId), companyId);
        SetNonPublicProperty(client, nameof(Client.Name), name);
        return client;
    }

    private static Driver CreateDriver(Guid id, Guid companyId, string name)
    {
        Driver driver = CreateUninitialized<Driver>();
        SetNonPublicProperty(driver, nameof(Driver.Id), id);
        SetNonPublicProperty(driver, nameof(Driver.CompanyId), companyId);
        SetNonPublicProperty(driver, nameof(Driver.Name), name);
        return driver;
    }

    private static Vehicle CreateVehicle(Guid id, Guid companyId, string brand, string model, string licensePlate)
    {
        Vehicle vehicle = CreateUninitialized<Vehicle>();
        SetNonPublicProperty(vehicle, nameof(Vehicle.Id), id);
        SetNonPublicProperty(vehicle, nameof(Vehicle.CompanyId), companyId);
        SetNonPublicProperty(vehicle, nameof(Vehicle.Brand), brand);
        SetNonPublicProperty(vehicle, nameof(Vehicle.Model), model);
        SetNonPublicProperty(vehicle, nameof(Vehicle.LicensePlate), licensePlate);
        return vehicle;
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GenerateRentalReceiptPdfQuery query = CreateValidQuery();
        _tenantProviderMock.Setup(tp => tp.UserId).Returns((Guid?)null);

        // act
        Result<PdfFileResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _receiptPdfGeneratorMock.Verify(g => g.Generate(It.IsAny<RentalReceiptPdfData>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GenerateRentalReceiptPdfQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<PdfFileResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _receiptPdfGeneratorMock.Verify(g => g.Generate(It.IsAny<RentalReceiptPdfData>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Rental_Does_Not_Exist()
    {
        // arrange
        GenerateRentalReceiptPdfQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(CreateCompanyUser(currentUserId, companyId));

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(query.RentalId)).ReturnsAsync((Rental?)null);

        // act
        Result<PdfFileResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(query.RentalId), Times.Once);
        _receiptPdfGeneratorMock.Verify(g => g.Generate(It.IsAny<RentalReceiptPdfData>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Rental_Belongs_To_Other_Company()
    {
        // arrange
        GenerateRentalReceiptPdfQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(CreateCompanyUser(currentUserId, companyId));

        Rental rentalFromOtherCompany = CreateRental(otherCompanyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        SetNonPublicProperty(rentalFromOtherCompany, nameof(Rental.IsCompleted), true);
        SetNonPublicProperty(rentalFromOtherCompany, nameof(Rental.ActualReturnDate), new DateOnly(2025, 1, 2));

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(query.RentalId)).ReturnsAsync(rentalFromOtherCompany);

        // act
        Result<PdfFileResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _clientRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _receiptPdfGeneratorMock.Verify(g => g.Generate(It.IsAny<RentalReceiptPdfData>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Client_Driver_Or_Vehicle_Belongs_To_Other_Company()
    {
        // arrange
        GenerateRentalReceiptPdfQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(CreateCompanyUser(currentUserId, companyId));

        Rental completedRental = CreateRental(companyId, clientId, driverId, vehicleId, planType: RentalPlanType.Daily);
        SetNonPublicProperty(completedRental, nameof(Rental.IsCompleted), true);
        SetNonPublicProperty(completedRental, nameof(Rental.ActualReturnDate), new DateOnly(2025, 1, 2));

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(query.RentalId)).ReturnsAsync(completedRental);

        Client clientFromOtherCompany = CreateClient(clientId, otherCompanyId, "Maria Silva");
        Driver driver = CreateDriver(driverId, companyId, "João Souza");
        Vehicle vehicle = CreateVehicle(vehicleId, companyId, "Ford", "Ka", "ABC1D23");

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(clientFromOtherCompany);
        _driverRepositoryMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        _vehicleRepositoryMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);

        // act
        Result<PdfFileResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _receiptPdfGeneratorMock.Verify(g => g.Generate(It.IsAny<RentalReceiptPdfData>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_PdfFileResult_When_Request_Is_Valid()
    {
        // arrange
        GenerateRentalReceiptPdfQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(CreateCompanyUser(currentUserId, companyId));

        Rental completedRental = CreateRental(companyId, clientId, driverId, vehicleId, planType: RentalPlanType.Daily);
        SetNonPublicProperty(completedRental, nameof(Rental.IsCompleted), true);
        SetNonPublicProperty(completedRental, nameof(Rental.ActualReturnDate), new DateOnly(2025, 1, 2));

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(query.RentalId)).ReturnsAsync(completedRental);

        Client client = CreateClient(clientId, companyId, "Maria Silva");
        Driver driver = CreateDriver(driverId, companyId, "João Souza");
        Vehicle vehicle = CreateVehicle(vehicleId, companyId, "Ford", "Ka", "ABC1D23");

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        _driverRepositoryMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        _vehicleRepositoryMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);

        byte[] expectedPdfBytes = new byte[] { 10, 20, 30 };
        RentalReceiptPdfData? capturedData = null;

        _receiptPdfGeneratorMock
            .Setup(g => g.Generate(It.IsAny<RentalReceiptPdfData>()))
            .Callback<RentalReceiptPdfData>(data => capturedData = data)
            .Returns(expectedPdfBytes);

        // act
        Result<PdfFileResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);

        Assert.AreEqual(expectedPdfBytes, result.Value.Content);
        Assert.IsTrue(result.Value.FileName.StartsWith("Recibo_Aluguel_"));
        Assert.IsTrue(result.Value.FileName.EndsWith(".pdf"));

        Assert.IsNotNull(capturedData);
        Assert.AreEqual("Diário", capturedData!.PlanTypeDisplayName);

        _receiptPdfGeneratorMock.Verify(g => g.Generate(It.IsAny<RentalReceiptPdfData>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_LogError_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        GenerateRentalReceiptPdfQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(CreateCompanyUser(currentUserId, companyId));

        _rentalRepositoryMock
            .Setup(r => r.GetByIdAsync(query.RentalId))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<PdfFileResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Ocorreu um erro ao gerar recibo PDF do aluguel")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}