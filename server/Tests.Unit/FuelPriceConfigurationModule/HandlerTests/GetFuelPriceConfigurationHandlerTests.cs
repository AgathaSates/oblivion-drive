using AutoMapper;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.FuelPriceConfigurationModule.DTOs;
using OblivionDrive.Application.FuelPriceConfigurationModule.Handlers;
using OblivionDrive.Application.FuelPriceConfigurationModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Tests.Unit.FuelPriceConfigurationModule.HandlerTests;

[TestClass]
[TestCategory("FuelPriceConfiguration - GetFuelPriceConfigurationHandler Unit Tests")]
public sealed class GetFuelPriceConfigurationHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = null!;
    private Mock<ITenantProvider> _tenantProviderMock = null!;
    private Mock<IRepositoryFuelPriceSettings> _fuelPriceSettingsRepositoryMock = null!;
    private Mock<IMapper> _mapperMock = null!;
    private Mock<ILogger<GetFuelPriceConfigurationQuery>> _loggerMock = null!;
    private GetFuelPriceConfigurationHandler _handler = null!;

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
            loggerUserManager.Object);

        _tenantProviderMock = new Mock<ITenantProvider>();

        _fuelPriceSettingsRepositoryMock = new Mock<IRepositoryFuelPriceSettings>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<GetFuelPriceConfigurationQuery>>();

        _handler = new GetFuelPriceConfigurationHandler(
            _tenantProviderMock.Object,
            _userManagerMock.Object,
            _fuelPriceSettingsRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    private static User CreateCompanyUser(Guid userId)
    {
        return new User
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = userId
        };
    }

    private static FuelPriceConfiguration CreateConfiguration(Guid companyId)
    {
        return new FuelPriceConfiguration(
            gasoline: 5.79m,
            gas: 4.10m,
            diesel: 6.20m,
            alcohol: 3.99m,
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        var query = new GetFuelPriceConfigurationQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<FuelPriceConfigurationDto> result =
            await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _fuelPriceSettingsRepositoryMock.Verify(r =>
            r.GetAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<FuelPriceConfigurationDto>(It.IsAny<FuelPriceConfiguration>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        var query = new GetFuelPriceConfigurationQuery();
        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<FuelPriceConfigurationDto> result =
            await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _fuelPriceSettingsRepositoryMock.Verify(r =>
            r.GetAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<FuelPriceConfigurationDto>(It.IsAny<FuelPriceConfiguration>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        var query = new GetFuelPriceConfigurationQuery();
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = currentUserId;

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _fuelPriceSettingsRepositoryMock
            .Setup(r => r.GetAsync(companyId))
            .ThrowsAsync(new Exception("Erro ao buscar configuração"));

        // act
        Result<FuelPriceConfigurationDto> result =
            await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro ao obter a configuração de preços de combustível")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Success_When_Configuration_Exists()
    {
        // arrange
        var query = new GetFuelPriceConfigurationQuery();
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = currentUserId;

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        FuelPriceConfiguration configuration = CreateConfiguration(companyId);

        _fuelPriceSettingsRepositoryMock
            .Setup(r => r.GetAsync(companyId))
            .ReturnsAsync(configuration);

        var expectedDto = new FuelPriceConfigurationDto(
            configuration.Gasoline,
            configuration.Gas,
            configuration.Diesel,
            configuration.Alcohol,
            configuration.LastUpdate);

        _mapperMock
            .Setup(m => m.Map<FuelPriceConfigurationDto>(configuration))
            .Returns(expectedDto);

        // act
        Result<FuelPriceConfigurationDto> result =
            await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        _fuelPriceSettingsRepositoryMock.Verify(r =>
            r.GetAsync(companyId), Times.Once);
        _mapperMock.Verify(m =>
            m.Map<FuelPriceConfigurationDto>(configuration), Times.Once);
    }
}