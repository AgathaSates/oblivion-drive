using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.DriverModule.DTOs;
using OblivionDrive.Application.DriverModule.Handlers;
using OblivionDrive.Application.DriverModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.DriverModule;

namespace OblivionDrive.Tests.Unit.DriverModule.HandlerTests;

[TestClass]
[TestCategory("Driver - GetAllDriversHandler Unit Tests")]
public class GetAllDriversHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryDriver> _driverRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetAllDriversHandler>> _loggerMock = default!;
    private Mock<IValidator<GetAllDriversQuery>> _validatorMock = default!;
    private GetAllDriversHandler _handler = default!;

    [TestInitialize]
    public void Setup()
    {
        var userStoreMock = new Mock<IUserStore<User>>();
        IOptions<IdentityOptions> identityOptions = Options.Create(new IdentityOptions());
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
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(Guid.NewGuid());

        _driverRepositoryMock = new Mock<IRepositoryDriver>();

        _mapperMock = new Mock<IMapper>();

        _loggerMock = new Mock<ILogger<GetAllDriversHandler>>();

        _validatorMock = new Mock<IValidator<GetAllDriversQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetAllDriversQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetAllDriversHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _driverRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetAllDriversQuery CreateQueryWithoutQuantity()
        => new GetAllDriversQuery(Quantity: null);

    private static GetAllDriversQuery CreateQueryWithQuantity(int quantity)
        => new GetAllDriversQuery(Quantity: quantity);

    private static User CreateCompanyUser(Guid id, Guid? companyId = null)
        => new User
        {
            Id = id,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? id
        };

    private static Driver CreateDriver(Guid companyId, string name)
        => new Driver(
            companyId: companyId,
            clientId: Guid.NewGuid(),
            name: name,
            phoneNumber: "47999999999",
            cpf: "12345678901",
            cnh: "1234567890",
            cnhExpirationDate: DateOnly.FromDateTime(DateTime.Today).AddDays(1),
            email: $"{name.Replace(" ", ".").ToLowerInvariant()}@email.com",
            isClientAlsoDriver: false);

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetAllDriversQuery query = CreateQueryWithoutQuantity();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetAllDriversQuery.Quantity), "A quantidade deve ser maior que zero.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<DriversResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailDriverDTO>>(It.IsAny<IReadOnlyCollection<Driver>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetAllDriversQuery query = CreateQueryWithoutQuantity();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<DriversResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailDriverDTO>>(It.IsAny<IReadOnlyCollection<Driver>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetAllDriversQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<DriversResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailDriverDTO>>(It.IsAny<IReadOnlyCollection<Driver>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_All_Drivers_When_Quantity_Is_Null()
    {
        // arrange
        GetAllDriversQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var drivers = new List<Driver>
        {
            CreateDriver(companyId, "Condutor 1"),
            CreateDriver(companyId, "Condutor 2"),
            CreateDriver(companyId, "Condutor 3"),
        };

        _driverRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(drivers);

        var detailList = new List<DetailDriverDTO>
        {
            new(drivers[0].Id, drivers[0].Name, drivers[0].Email, drivers[0].PhoneNumber, drivers[0].Cpf, drivers[0].Cnh, drivers[0].CnhExpirationDate, drivers[0].ClientId, drivers[0].IsClientAlsoDriver),
            new(drivers[1].Id, drivers[1].Name, drivers[1].Email, drivers[1].PhoneNumber, drivers[1].Cpf, drivers[1].Cnh, drivers[1].CnhExpirationDate, drivers[1].ClientId, drivers[1].IsClientAlsoDriver),
            new(drivers[2].Id, drivers[2].Name, drivers[2].Email, drivers[2].PhoneNumber, drivers[2].Cpf, drivers[2].Cnh, drivers[2].CnhExpirationDate, drivers[2].ClientId, drivers[2].IsClientAlsoDriver),
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailDriverDTO>>(drivers))
            .Returns(detailList);

        // act
        Result<DriversResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.IsNotNull(result.Value.Drivers);
        Assert.AreEqual(detailList.Count, result.Value.Drivers.Count);
        CollectionAssert.AreEquivalent(detailList, result.Value.Drivers.ToList());

        _driverRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);
        _driverRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailDriverDTO>>(drivers), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Call_GetAll_With_Quantity_When_Quantity_Is_Provided()
    {
        // arrange
        const int quantity = 2;
        GetAllDriversQuery query = CreateQueryWithQuantity(quantity);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var drivers = new List<Driver>
        {
            CreateDriver(companyId, "Condutor 1"),
            CreateDriver(companyId, "Condutor 2"),
        };

        _driverRepositoryMock
            .Setup(r => r.GetAllAsync(quantity))
            .ReturnsAsync(drivers);

        var detailList = new List<DetailDriverDTO>
        {
            new(drivers[0].Id, drivers[0].Name, drivers[0].Email, drivers[0].PhoneNumber, drivers[0].Cpf, drivers[0].Cnh, drivers[0].CnhExpirationDate, drivers[0].ClientId, drivers[0].IsClientAlsoDriver),
            new(drivers[1].Id, drivers[1].Name, drivers[1].Email, drivers[1].PhoneNumber, drivers[1].Cpf, drivers[1].Cnh, drivers[1].CnhExpirationDate, drivers[1].ClientId, drivers[1].IsClientAlsoDriver),
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailDriverDTO>>(drivers))
            .Returns(detailList);

        // act
        Result<DriversResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(detailList.Count, result.Value.Drivers.Count);

        _driverRepositoryMock.Verify(r =>
            r.GetAllAsync(quantity), Times.Once);
        _driverRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailDriverDTO>>(drivers), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_And_LogError_When_Exception_Occurs()
    {
        // arrange
        GetAllDriversQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<DriversResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a listagem de condutores da empresa")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}