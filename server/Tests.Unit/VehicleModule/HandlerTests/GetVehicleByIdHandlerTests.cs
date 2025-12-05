using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.VehicleModule.DTOs;
using OblivionDrive.Application.VehicleModule.Handlers;
using OblivionDrive.Application.VehicleModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Tests.Unit.VehicleModule.HandlerTests;

[TestClass]
[TestCategory("Vehicle - GetVehicleByIdHandler Unit Tests")]
public class GetVehicleByIdHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryVehicle> _vehicleRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetVehicleByIdHandler>> _loggerMock = default!;
    private Mock<IValidator<GetVehicleByIdQuery>> _validatorMock = default!;
    private GetVehicleByIdHandler _handler = default!;

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

        _vehicleRepositoryMock = new Mock<IRepositoryVehicle>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<GetVehicleByIdHandler>>();

        _validatorMock = new Mock<IValidator<GetVehicleByIdQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetVehicleByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetVehicleByIdHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _vehicleRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetVehicleByIdQuery CreateValidQuery()
    {
        return new GetVehicleByIdQuery(
            VehicleId: Guid.NewGuid()
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

    private static Vehicle CreateVehicle(Guid companyId)
    {
        Guid vehicleGroupId = Guid.NewGuid();

        var vehicle = new Vehicle(
            licensePlate: "ABC1D23",
            brand: "Toyota",
            model: "Corolla",
            color: "White",
            fuelType: FuelType.Gasoline,
            fuelTankCapacityInLiters: 55.5m,
            year: DateTime.UtcNow.Year,
            vehicleGroupId: vehicleGroupId,
            companyId: companyId);

        vehicle.SetPhoto(new byte[] { 1, 2, 3 });

        return vehicle;
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetVehicleByIdQuery query = CreateValidQuery();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetVehicleByIdQuery.VehicleId), "O identificador do veículo é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<DetailVehicleDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailVehicleDTO>(It.IsAny<Vehicle>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetVehicleByIdQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<DetailVehicleDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailVehicleDTO>(It.IsAny<Vehicle>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetVehicleByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<DetailVehicleDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailVehicleDTO>(It.IsAny<Vehicle>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Vehicle_Does_Not_Exist()
    {
        // arrange
        GetVehicleByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleRepositoryMock
            .Setup(r => r.GetByIdAsync(query.VehicleId))
            .ReturnsAsync((Vehicle?)null);

        // act
        Result<DetailVehicleDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.VehicleId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailVehicleDTO>(It.IsAny<Vehicle>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Vehicle_Belongs_To_Other_Company()
    {
        // arrange
        GetVehicleByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Vehicle otherCompanyVehicle = CreateVehicle(otherCompanyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleRepositoryMock
            .Setup(r => r.GetByIdAsync(query.VehicleId))
            .ReturnsAsync(otherCompanyVehicle);

        // act
        Result<DetailVehicleDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.VehicleId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailVehicleDTO>(It.IsAny<Vehicle>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Success_When_Vehicle_Is_Found_And_Belongs_To_Company()
    {
        // arrange
        GetVehicleByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Vehicle existingVehicle = CreateVehicle(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleRepositoryMock
            .Setup(r => r.GetByIdAsync(query.VehicleId))
            .ReturnsAsync(existingVehicle);

        var expectedDto = new DetailVehicleDTO(
            Id: existingVehicle.Id,
            LicensePlate: existingVehicle.LicensePlate,
            Brand: existingVehicle.Brand,
            Model: existingVehicle.Model,
            Color: existingVehicle.Color,
            FuelType: existingVehicle.FuelType,
            FuelTankCapacityInLiters: existingVehicle.FuelTankCapacityInLiters,
            Year: existingVehicle.Year,
            VehicleGroupId: existingVehicle.VehicleGroupId,
            PhotoBytes: existingVehicle.PhotoBytes ?? Array.Empty<byte>());

        _mapperMock
            .Setup(m => m.Map<DetailVehicleDTO>(existingVehicle))
            .Returns(expectedDto);

        // act
        Result<DetailVehicleDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.VehicleId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailVehicleDTO>(existingVehicle), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InternalExceptionError_When_Exception_Occurs()
    {
        // arrange
        GetVehicleByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleRepositoryMock
            .Setup(r => r.GetByIdAsync(query.VehicleId))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<DetailVehicleDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a obtenção de detalhes do veículo")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}