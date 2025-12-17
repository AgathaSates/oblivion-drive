using System.Reflection;
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
using OblivionDrive.Application.RentalModule.Services;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Tests.Unit.RentalModule.HandlerTests;

[TestClass]
[TestCategory("Rental - SendRentalReceiptEmailHandler Unit Tests")]
public class SendRentalReceiptEmailHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;
    private Mock<IRepositoryClient> _clientRepositoryMock = default!;
    private Mock<IRepositoryDriver> _driverRepositoryMock = default!;
    private Mock<IRepositoryVehicle> _vehicleRepositoryMock = default!;
    private Mock<IRentalReceiptPdfGenerator> _receiptPdfGeneratorMock = default!;
    private Mock<IEmailSender> _emailSenderMock = default!;
    private Mock<IValidator<SendRentalReceiptEmailCommand>> _validatorMock = default!;
    private Mock<ILogger<SendRentalReceiptEmailHandler>> _loggerMock = default!;

    private SendRentalReceiptEmailHandler _handler = default!;

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
        _receiptPdfGeneratorMock
            .Setup(g => g.Generate(It.IsAny<RentalReceiptPdfData>()))
            .Returns(Array.Empty<byte>());

        _emailSenderMock = new Mock<IEmailSender>();
        _emailSenderMock
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<SendRentalReceiptEmailCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<SendRentalReceiptEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<SendRentalReceiptEmailHandler>>();

        _handler = new SendRentalReceiptEmailHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _rentalRepositoryMock.Object,
            _clientRepositoryMock.Object,
            _driverRepositoryMock.Object,
            _vehicleRepositoryMock.Object,
            _receiptPdfGeneratorMock.Object,
            _emailSenderMock.Object,
            _validatorMock.Object,
            _loggerMock.Object
        );
    }

    private static SendRentalReceiptEmailCommand CreateValidCommand()
        => new(RentalId: Guid.NewGuid(), Email: "cliente@exemplo.com");

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
        => new()
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? userId
        };

    private static Rental CreateRental(Guid companyId, Guid clientId, Guid driverId, Guid vehicleId)
        => new(
            companyId: companyId,
            clientId: clientId,
            driverId: driverId,
            vehicleId: vehicleId,
            planType: RentalPlanType.Daily,
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

    private static Client CreateClient(Guid clientId, string name)
    {
        Client client = CreateUninitialized<Client>();
        SetNonPublicProperty(client, nameof(Client.Id), clientId);
        SetNonPublicProperty(client, nameof(Client.Name), name);
        return client;
    }

    private static Driver CreateDriver(Guid driverId, string name)
    {
        Driver driver = CreateUninitialized<Driver>();
        SetNonPublicProperty(driver, nameof(Driver.Id), driverId);
        SetNonPublicProperty(driver, nameof(Driver.Name), name);
        return driver;
    }

    private static Vehicle CreateVehicle(Guid vehicleId, string brand, string model, string licensePlate)
    {
        Vehicle vehicle = CreateUninitialized<Vehicle>();
        SetNonPublicProperty(vehicle, nameof(Vehicle.Id), vehicleId);
        SetNonPublicProperty(vehicle, nameof(Vehicle.Brand), brand);
        SetNonPublicProperty(vehicle, nameof(Vehicle.Model), model);
        SetNonPublicProperty(vehicle, nameof(Vehicle.LicensePlate), licensePlate);
        return vehicle;
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        SendRentalReceiptEmailCommand command = CreateValidCommand();

        var failures = new List<ValidationFailure>
        {
            new(nameof(SendRentalReceiptEmailCommand.Email), "O e-mail é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _receiptPdfGeneratorMock.Verify(g => g.Generate(It.IsAny<RentalReceiptPdfData>()), Times.Never);
        _emailSenderMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        SendRentalReceiptEmailCommand command = CreateValidCommand();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns((Guid?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _emailSenderMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        SendRentalReceiptEmailCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(currentUserId.ToString()), Times.Once);
        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _emailSenderMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Rental_Does_Not_Exist()
    {
        // arrange
        SendRentalReceiptEmailCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(CreateCompanyUser(currentUserId, companyId));

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(command.RentalId)).ReturnsAsync((Rental?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(command.RentalId), Times.Once);
        _receiptPdfGeneratorMock.Verify(g => g.Generate(It.IsAny<RentalReceiptPdfData>()), Times.Never);
        _emailSenderMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Rental_Belongs_To_Other_Company()
    {
        // arrange
        SendRentalReceiptEmailCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(CreateCompanyUser(currentUserId, companyId));

        Rental rentalFromOtherCompany = CreateRental(otherCompanyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        SetNonPublicProperty(rentalFromOtherCompany, nameof(Rental.IsCompleted), true);
        SetNonPublicProperty(rentalFromOtherCompany, nameof(Rental.ActualReturnDate), new DateOnly(2025, 1, 2));

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(command.RentalId)).ReturnsAsync(rentalFromOtherCompany);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _clientRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _driverRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _emailSenderMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Send_Email_And_Return_Success_When_Request_Is_Valid()
    {
        // arrange
        SendRentalReceiptEmailCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(CreateCompanyUser(currentUserId, companyId));

        Rental completedRental = CreateRental(companyId, clientId, driverId, vehicleId);
        SetNonPublicProperty(completedRental, nameof(Rental.IsCompleted), true);
        SetNonPublicProperty(completedRental, nameof(Rental.ActualReturnDate), new DateOnly(2025, 1, 2));
        SetNonPublicProperty(completedRental, nameof(Rental.FinalAmountToPay), 120m);
        SetNonPublicProperty(completedRental, nameof(Rental.GrossRentalAmount), 220m);

        _rentalRepositoryMock.Setup(r => r.GetByIdAsync(command.RentalId)).ReturnsAsync(completedRental);

        Client client = CreateClient(clientId, name: "Maria Silva");
        Driver driver = CreateDriver(driverId, name: "João Souza");
        Vehicle vehicle = CreateVehicle(vehicleId, brand: "Ford", model: "Ka", licensePlate: "ABC1D23");

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        _driverRepositoryMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        _vehicleRepositoryMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);

        byte[] expectedPdfBytes = new byte[] { 1, 2, 3 };
        RentalReceiptPdfData? capturedReceiptData = null;

        _receiptPdfGeneratorMock
            .Setup(g => g.Generate(It.IsAny<RentalReceiptPdfData>()))
            .Callback<RentalReceiptPdfData>(data => capturedReceiptData = data)
            .Returns(expectedPdfBytes);

        EmailMessage? capturedMessage = null;

        _emailSenderMock
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => capturedMessage = message)
            .Returns(Task.CompletedTask);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        Assert.IsNotNull(capturedReceiptData);
        Assert.AreEqual(completedRental.Id, capturedReceiptData!.RentalId);

        Assert.IsNotNull(capturedMessage);
        Assert.AreEqual(command.Email, capturedMessage!.To);
        Assert.IsTrue(capturedMessage.Subject.Contains(completedRental.Id.ToString("N")));

        Assert.IsNotNull(capturedMessage.Attachments);

        Assert.AreEqual(1, capturedMessage.Attachments.Count);

        EmailAttachment attachment = capturedMessage.Attachments.Single();

        Assert.AreEqual("application/pdf", attachment.ContentType);

        CollectionAssert.AreEqual(expectedPdfBytes, attachment.Content);

        _emailSenderMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_LogError_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        SendRentalReceiptEmailCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(CreateCompanyUser(currentUserId, companyId));

        _rentalRepositoryMock
            .Setup(r => r.GetByIdAsync(command.RentalId))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Erro ao enviar recibo por e-mail")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
