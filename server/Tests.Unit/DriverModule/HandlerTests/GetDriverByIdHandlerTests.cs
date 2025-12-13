
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
[TestCategory("Driver - GetDriverByIdHandler Unit Tests")]
public class GetDriverByIdHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryDriver> _driverRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetDriverByIdHandler>> _loggerMock = default!;
    private Mock<IValidator<GetDriverByIdQuery>> _validatorMock = default!;
    private GetDriverByIdHandler _handler = default!;

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
        _mapperMock
            .Setup(m => m.Map<DetailDriverDTO>(It.IsAny<Driver>()))
            .Returns(default(DetailDriverDTO)!);

        _loggerMock = new Mock<ILogger<GetDriverByIdHandler>>();

        _validatorMock = new Mock<IValidator<GetDriverByIdQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetDriverByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetDriverByIdHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _driverRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetDriverByIdQuery CreateValidQuery()
        => new GetDriverByIdQuery(DriverId: Guid.NewGuid());

    private static User CreateCompanyUser(Guid id, Guid? companyId = null)
        => new User
        {
            Id = id,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? id
        };

    private static Driver CreateDriver(Guid companyId)
        => new Driver(
            companyId: companyId,
            clientId: Guid.NewGuid(),
            name: "Condutor",
            phoneNumber: "47999999999",
            cpf: "12345678901",
            cnh: "1234567890",
            cnhExpirationDate: DateOnly.FromDateTime(DateTime.Today).AddDays(1),
            email: "condutor@email.com",
            isClientAlsoDriver: false);

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetDriverByIdQuery query = CreateValidQuery();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetDriverByIdQuery.DriverId), "O identificador do condutor é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<DetailDriverDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailDriverDTO>(It.IsAny<Driver>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetDriverByIdQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<DetailDriverDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailDriverDTO>(It.IsAny<Driver>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetDriverByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<DetailDriverDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailDriverDTO>(It.IsAny<Driver>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Driver_Does_Not_Exist()
    {
        // arrange
        GetDriverByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetByIdAsync(query.DriverId))
            .ReturnsAsync((Driver?)null);

        // act
        Result<DetailDriverDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.DriverId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailDriverDTO>(It.IsAny<Driver>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Driver_Belongs_To_Other_Company()
    {
        // arrange
        GetDriverByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Driver driverFromOtherCompany = CreateDriver(otherCompanyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetByIdAsync(query.DriverId))
            .ReturnsAsync(driverFromOtherCompany);

        // act
        Result<DetailDriverDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.DriverId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailDriverDTO>(It.IsAny<Driver>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_DriverDetail_When_Driver_Exists_And_Belongs_To_Current_Company()
    {
        // arrange
        GetDriverByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Driver existingDriver = CreateDriver(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetByIdAsync(query.DriverId))
            .ReturnsAsync(existingDriver);

        var expectedDetail = new DetailDriverDTO(
            Id: existingDriver.Id,
            Name: existingDriver.Name,
            Email: existingDriver.Email,
            PhoneNumber: existingDriver.PhoneNumber,
            Cpf: existingDriver.Cpf,
            Cnh: existingDriver.Cnh,
            CnhExpirationDate: existingDriver.CnhExpirationDate,
            ClientId: existingDriver.ClientId,
            IsClientAlsoDriver: existingDriver.IsClientAlsoDriver
        );

        _mapperMock
            .Setup(m => m.Map<DetailDriverDTO>(existingDriver))
            .Returns(expectedDetail);

        // act
        Result<DetailDriverDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDetail, result.Value);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.DriverId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailDriverDTO>(existingDriver), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_And_LogError_When_Exception_Occurs()
    {
        // arrange
        GetDriverByIdQuery query = CreateValidQuery();

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
            .Setup(r => r.GetByIdAsync(query.DriverId))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<DetailDriverDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a obtenção de detalhes do condutor")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}