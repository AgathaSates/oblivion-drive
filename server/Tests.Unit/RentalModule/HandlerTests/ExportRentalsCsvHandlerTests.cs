using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.RentalModule.Handlers;
using OblivionDrive.Application.RentalModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Tests.Unit.RentalModule.HandlerTests;

[TestClass]
[TestCategory("Rental - ExportRentalsCsvHandler Unit Tests")]
public class ExportRentalsCsvHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;
    private Mock<ILogger<ExportRentalsCsvHandler>> _loggerMock = default!;

    private ExportRentalsCsvHandler _handler = default!;

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
        _loggerMock = new Mock<ILogger<ExportRentalsCsvHandler>>();

        _handler = new ExportRentalsCsvHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _rentalRepositoryMock.Object,
            _loggerMock.Object
        );
    }

    private static ExportRentalsCsvQuery CreateValidQuery()
        => new(Quantity: 10);

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
        => new()
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? userId
        };

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

    private static RentalSummaryRow CreateSummaryRow(
        Guid rentalId,
        string clientName,
        string vehicleBrand,
        string vehicleModel,
        string vehiclePlate,
        RentalPlanType planType,
        DateOnly startDate,
        DateOnly expectedReturnDate,
        DateOnly? actualReturnDate,
        bool isCompleted,
        decimal grossAmount,
        decimal finalAmountToPay)
    {
        RentalSummaryRow row = CreateUninitialized<RentalSummaryRow>();

        SetNonPublicProperty(row, nameof(RentalSummaryRow.RentalId), rentalId);
        SetNonPublicProperty(row, nameof(RentalSummaryRow.ClientName), clientName);
        SetNonPublicProperty(row, nameof(RentalSummaryRow.VehicleBrand), vehicleBrand);
        SetNonPublicProperty(row, nameof(RentalSummaryRow.VehicleModel), vehicleModel);
        SetNonPublicProperty(row, nameof(RentalSummaryRow.VehicleLicensePlate), vehiclePlate);

        SetNonPublicProperty(row, nameof(RentalSummaryRow.PlanType), planType);

        SetNonPublicProperty(row, nameof(RentalSummaryRow.StartDate), startDate);
        SetNonPublicProperty(row, nameof(RentalSummaryRow.ActualReturnDate), actualReturnDate);

        SetNonPublicProperty(row, nameof(RentalSummaryRow.IsCompleted), isCompleted);

        SetNonPublicProperty(row, nameof(RentalSummaryRow.GrossRentalAmount), grossAmount);
        SetNonPublicProperty(row, nameof(RentalSummaryRow.FinalAmountToPay), finalAmountToPay);

        return row;
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        ExportRentalsCsvQuery query = CreateValidQuery();
        _tenantProviderMock.Setup(tp => tp.UserId).Returns((Guid?)null);

        // act
        Result<(byte[] Content, string FileName)> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetSummaryRowsByCompanyIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        ExportRentalsCsvQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<(byte[] Content, string FileName)> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r => r.GetSummaryRowsByCompanyIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Csv_With_Header_And_Escaped_Values_When_Request_Is_Valid()
    {
        // arrange
        ExportRentalsCsvQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(CreateCompanyUser(currentUserId, companyId));

        Guid rentalId = Guid.NewGuid();

        string clientName = "Cliente; \"VIP\"";

        List<RentalSummaryRow> rows = new()
        {
            CreateSummaryRow(
                rentalId: rentalId,
                clientName: clientName,
                vehicleBrand: "Ford",
                vehicleModel: "Ka",
                vehiclePlate: "ABC1D23",
                planType: RentalPlanType.Daily,
                startDate: new DateOnly(2025, 1, 1),
                expectedReturnDate: new DateOnly(2025, 1, 2),
                actualReturnDate: new DateOnly(2025, 1, 2),
                isCompleted: true,
                grossAmount: 200m,
                finalAmountToPay: 50m
            )
        };

        _rentalRepositoryMock
            .Setup(r => r.GetSummaryRowsByCompanyIdAsync(companyId, query.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        // act
        Result<(byte[] Content, string FileName)> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value.Content);
        Assert.IsTrue(result.Value.FileName.StartsWith("Alugueis_"));
        Assert.IsTrue(result.Value.FileName.EndsWith(".csv"));

        string csvText = Encoding.UTF8.GetString(result.Value.Content);

        csvText = csvText.TrimStart('\uFEFF');

        Assert.IsTrue(csvText.StartsWith("sep=;\n") || csvText.StartsWith("sep=;\r\n"));

        Assert.IsTrue(csvText.Contains("AluguelId;Cliente;Veículo;Plano;Saída;PrevRetorno;Devolução;Status;TotalBruto;ValorFinalAPagar"));

        Assert.IsTrue(csvText.Contains("\"Cliente; \"\"VIP\"\"\""));

        _rentalRepositoryMock.Verify(r => r.GetSummaryRowsByCompanyIdAsync(companyId, query.Quantity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_LogError_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        ExportRentalsCsvQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(CreateCompanyUser(currentUserId, companyId));

        _rentalRepositoryMock
            .Setup(r => r.GetSummaryRowsByCompanyIdAsync(companyId, query.Quantity, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<(byte[] Content, string FileName)> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Erro ao exportar aluguéis CSV")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
