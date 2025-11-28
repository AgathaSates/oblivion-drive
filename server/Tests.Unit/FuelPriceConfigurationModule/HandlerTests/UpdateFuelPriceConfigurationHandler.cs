using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.FuelPriceConfigurationModule.Commands;
using OblivionDrive.Application.FuelPriceConfigurationModule.DTOs;
using OblivionDrive.Application.FuelPriceConfigurationModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.FuelPriceConfigurationModule.HandlerTests;

[TestClass]
[TestCategory("FuelPriceConfiguration - UpdateFuelPriceConfigurationHandler Unit Tests")]
public sealed class UpdateFuelPriceConfigurationHandlerTests
{
    private Mock<IValidator<UpdateFuelPriceConfigurationCommand>> _validatorMock = null!;
    private Mock<ITenantProvider> _tenantProviderMock = null!;
    private Mock<UserManager<User>> _userManagerMock = null!;
    private Mock<IRepositoryFuelPriceSettings> _fuelPriceSettingsRepositoryMock = null!;
    private Mock<IMapper> _mapperMock = null!;
    private Mock<ILogger<UpdateFuelPriceConfigurationCommand>> _loggerMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private UpdateFuelPriceConfigurationHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _validatorMock = new Mock<IValidator<UpdateFuelPriceConfigurationCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateFuelPriceConfigurationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(Guid.NewGuid());

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

        _fuelPriceSettingsRepositoryMock = new Mock<IRepositoryFuelPriceSettings>();

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);

        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<UpdateFuelPriceConfigurationCommand>>();

        _handler = new UpdateFuelPriceConfigurationHandler(
            _validatorMock.Object,
            _tenantProviderMock.Object,
            _userManagerMock.Object,
            _fuelPriceSettingsRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _unitOfWorkMock.Object);
    }

    private static UpdateFuelPriceConfigurationCommand CreateValidCommand()
    {
        return new UpdateFuelPriceConfigurationCommand(
            Gasoline: 5.79m,
            Gas: 4.10m,
            Diesel: 6.20m,
            Alcohol: 3.99m);
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
            gasoline: 0m,
            gas: 0m,
            diesel: 0m,
            alcohol: 0m,
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        var command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(UpdateFuelPriceConfigurationCommand.Gasoline),
                "O preço da gasolina deve ser maior que zero.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<FuelPriceConfigurationDto> result =
            await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _fuelPriceSettingsRepositoryMock.Verify(r =>
            r.GetAsync(It.IsAny<Guid>()), Times.Never);
        _fuelPriceSettingsRepositoryMock.Verify(r =>
            r.SaveAsync(It.IsAny<FuelPriceConfiguration>(), It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        var command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<FuelPriceConfigurationDto> result =
            await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _fuelPriceSettingsRepositoryMock.Verify(r =>
            r.GetAsync(It.IsAny<Guid>()), Times.Never);
        _fuelPriceSettingsRepositoryMock.Verify(r =>
            r.SaveAsync(It.IsAny<FuelPriceConfiguration>(), It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        var command = CreateValidCommand();
        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<FuelPriceConfigurationDto> result =
            await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _fuelPriceSettingsRepositoryMock.Verify(r =>
            r.GetAsync(It.IsAny<Guid>()), Times.Never);
        _fuelPriceSettingsRepositoryMock.Verify(r =>
            r.SaveAsync(It.IsAny<FuelPriceConfiguration>(), It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Company()
    {
        // arrange
        var command = CreateValidCommand();
        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        var nonCompanyUser = new User
        {
            Id = currentUserId,
            UserName = "employeeUser",
            Email = "employee@example.com",
            UserType = UserType.Employee
        };

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(nonCompanyUser);

        // act
        Result<FuelPriceConfigurationDto> result =
            await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _fuelPriceSettingsRepositoryMock.Verify(r =>
            r.GetAsync(It.IsAny<Guid>()), Times.Never);
        _fuelPriceSettingsRepositoryMock.Verify(r =>
            r.SaveAsync(It.IsAny<FuelPriceConfiguration>(), It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_When_Exception_Occurs_During_Save()
    {
        // arrange
        var command = CreateValidCommand();
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = currentUserId;

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        FuelPriceConfiguration existingConfiguration = CreateConfiguration(companyId);

        _fuelPriceSettingsRepositoryMock
            .Setup(r => r.GetAsync(companyId))
            .ReturnsAsync(existingConfiguration);

        _fuelPriceSettingsRepositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<FuelPriceConfiguration>(), companyId))
            .ThrowsAsync(new Exception("Erro ao salvar configuração"));

        // act
        Result<FuelPriceConfigurationDto> result =
            await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro ao atualizar a configuração de preços de combustível")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Update_Configuration_And_Return_Success()
    {
        // arrange
        var command = CreateValidCommand();
        Guid currentUserId = Guid.NewGuid();
        Guid companyId = currentUserId;

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        FuelPriceConfiguration existingConfiguration = CreateConfiguration(companyId);

        _fuelPriceSettingsRepositoryMock
            .Setup(r => r.GetAsync(companyId))
            .ReturnsAsync(existingConfiguration);

        FuelPriceConfiguration? capturedConfiguration = null;

        _fuelPriceSettingsRepositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<FuelPriceConfiguration>(), companyId))
            .Callback<FuelPriceConfiguration, Guid>((config, _) =>
            {
                capturedConfiguration = config;
            })
            .Returns(Task.CompletedTask);

        var expectedDto = new FuelPriceConfigurationDto(
            command.Gasoline,
            command.Gas,
            command.Diesel,
            command.Alcohol,
            LastUpdate: DateOnly.FromDateTime(DateTime.Now));

        _mapperMock
            .Setup(m => m.Map<FuelPriceConfigurationDto>(It.IsAny<FuelPriceConfiguration>()))
            .Returns(expectedDto);

        // act
        Result<FuelPriceConfigurationDto> result =
            await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);

        Assert.AreEqual(expectedDto.Gasoline, result.Value.Gasoline);
        Assert.AreEqual(expectedDto.Gas, result.Value.Gas);
        Assert.AreEqual(expectedDto.Diesel, result.Value.Diesel);
        Assert.AreEqual(expectedDto.Alcohol, result.Value.Alcohol);

        Assert.IsNotNull(capturedConfiguration);
        Assert.AreEqual(command.Gasoline, capturedConfiguration!.Gasoline);
        Assert.AreEqual(command.Gas, capturedConfiguration.Gas);
        Assert.AreEqual(command.Diesel, capturedConfiguration.Diesel);
        Assert.AreEqual(command.Alcohol, capturedConfiguration.Alcohol);
        Assert.AreEqual(companyId, capturedConfiguration.CompanyId);

        _fuelPriceSettingsRepositoryMock.Verify(r =>
            r.SaveAsync(It.IsAny<FuelPriceConfiguration>(), companyId), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
    }
}