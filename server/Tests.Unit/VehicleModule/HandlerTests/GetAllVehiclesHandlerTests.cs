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
[TestCategory("Vehicle - GetAllVehiclesHandler Unit Tests")]
public class GetAllVehiclesHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryVehicle> _vehicleRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetAllVehiclesHandler>> _loggerMock = default!;
    private Mock<IValidator<GetAllVehiclesQuery>> _validatorMock = default!;
    private GetAllVehiclesHandler _handler = default!;

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
        _loggerMock = new Mock<ILogger<GetAllVehiclesHandler>>();

        _validatorMock = new Mock<IValidator<GetAllVehiclesQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetAllVehiclesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetAllVehiclesHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _vehicleRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetAllVehiclesQuery CreateValidQuery(Guid? vehicleGroupId = null, int? quantity = 10)
    {
        return new GetAllVehiclesQuery(
            VehicleGroupId: vehicleGroupId,
            Quantity: quantity
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

    private static Vehicle CreateVehicle(Guid companyId, Guid vehicleGroupId, string licensePlate)
    {
        var vehicle = new Vehicle(
            licensePlate: licensePlate,
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
        GetAllVehiclesQuery query = CreateValidQuery();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetAllVehiclesQuery.Quantity), "A quantidade deve ser maior que zero.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<VehiclesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetByVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailVehicleDTO>>(It.IsAny<IReadOnlyCollection<Vehicle>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetAllVehiclesQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<VehiclesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetByVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailVehicleDTO>>(It.IsAny<IReadOnlyCollection<Vehicle>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetAllVehiclesQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<VehiclesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetByVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailVehicleDTO>>(It.IsAny<IReadOnlyCollection<Vehicle>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Success_When_Quantity_And_VehicleGroup_Are_Null()
    {
        // arrange
        GetAllVehiclesQuery query = CreateValidQuery(vehicleGroupId: null, quantity: null);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var vehicles = new List<Vehicle>
        {
            CreateVehicle(companyId, vehicleGroupId, "AAA1A11"),
            CreateVehicle(companyId, vehicleGroupId, "BBB2B22")
        };

        _vehicleRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(vehicles);

        var expectedDtos = new List<DetailVehicleDTO>
        {
            new(
                Id: vehicles[0].Id,
                LicensePlate: vehicles[0].LicensePlate,
                Brand: vehicles[0].Brand,
                Model: vehicles[0].Model,
                Color: vehicles[0].Color,
                FuelType: vehicles[0].FuelType,
                FuelTankCapacityInLiters: vehicles[0].FuelTankCapacityInLiters,
                Year: vehicles[0].Year,
                VehicleGroupId: vehicles[0].VehicleGroupId,
                PhotoBytes: vehicles[0].PhotoBytes ?? Array.Empty<byte>()),
            new(
                Id: vehicles[1].Id,
                LicensePlate: vehicles[1].LicensePlate,
                Brand: vehicles[1].Brand,
                Model: vehicles[1].Model,
                Color: vehicles[1].Color,
                FuelType: vehicles[1].FuelType,
                FuelTankCapacityInLiters: vehicles[1].FuelTankCapacityInLiters,
                Year: vehicles[1].Year,
                VehicleGroupId: vehicles[1].VehicleGroupId,
                PhotoBytes: vehicles[1].PhotoBytes ?? Array.Empty<byte>())
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailVehicleDTO>>(vehicles))
            .Returns(expectedDtos);

        // act
        Result<VehiclesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(2, result.Value.Vehicles.Count);
        CollectionAssert.AreEquivalent(
            expectedDtos,
            result.Value.Vehicles.ToList());

        _vehicleRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);
        _vehicleRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetByVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailVehicleDTO>>(vehicles), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Success_When_Quantity_Is_Specified_And_VehicleGroup_Is_Null()
    {
        // arrange
        const int quantity = 5;
        GetAllVehiclesQuery query = CreateValidQuery(vehicleGroupId: null, quantity: quantity);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var vehicles = new List<Vehicle>
        {
            CreateVehicle(companyId, vehicleGroupId, "AAA1A11"),
            CreateVehicle(companyId, vehicleGroupId, "BBB2B22")
        };

        _vehicleRepositoryMock
            .Setup(r => r.GetAllAsync(quantity))
            .ReturnsAsync(vehicles);

        var expectedDtos = new List<DetailVehicleDTO>
        {
            new(
                Id: vehicles[0].Id,
                LicensePlate: vehicles[0].LicensePlate,
                Brand: vehicles[0].Brand,
                Model: vehicles[0].Model,
                Color: vehicles[0].Color,
                FuelType: vehicles[0].FuelType,
                FuelTankCapacityInLiters: vehicles[0].FuelTankCapacityInLiters,
                Year: vehicles[0].Year,
                VehicleGroupId: vehicles[0].VehicleGroupId,
                PhotoBytes: vehicles[0].PhotoBytes ?? Array.Empty<byte>()),
            new(
                Id: vehicles[1].Id,
                LicensePlate: vehicles[1].LicensePlate,
                Brand: vehicles[1].Brand,
                Model: vehicles[1].Model,
                Color: vehicles[1].Color,
                FuelType: vehicles[1].FuelType,
                FuelTankCapacityInLiters: vehicles[1].FuelTankCapacityInLiters,
                Year: vehicles[1].Year,
                VehicleGroupId: vehicles[1].VehicleGroupId,
                PhotoBytes: vehicles[1].PhotoBytes ?? Array.Empty<byte>())
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailVehicleDTO>>(vehicles))
            .Returns(expectedDtos);

        // act
        Result<VehiclesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(2, result.Value.Vehicles.Count);
        CollectionAssert.AreEquivalent(
            expectedDtos,
            result.Value.Vehicles.ToList());

        _vehicleRepositoryMock.Verify(r =>
            r.GetAllAsync(quantity), Times.Once);
        _vehicleRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetByVehicleGroupAsync(It.IsAny<Guid>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailVehicleDTO>>(vehicles), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Success_When_VehicleGroup_Is_Specified()
    {
        // arrange
        Guid vehicleGroupId = Guid.NewGuid();
        GetAllVehiclesQuery query = CreateValidQuery(vehicleGroupId: vehicleGroupId, quantity: 10);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var vehicles = new List<Vehicle>
        {
            CreateVehicle(companyId, vehicleGroupId, "AAA1A11"),
            CreateVehicle(companyId, vehicleGroupId, "BBB2B22")
        };

        _vehicleRepositoryMock
            .Setup(r => r.GetByVehicleGroupAsync(vehicleGroupId))
            .ReturnsAsync(vehicles);

        var expectedDtos = new List<DetailVehicleDTO>
        {
            new(
                Id: vehicles[0].Id,
                LicensePlate: vehicles[0].LicensePlate,
                Brand: vehicles[0].Brand,
                Model: vehicles[0].Model,
                Color: vehicles[0].Color,
                FuelType: vehicles[0].FuelType,
                FuelTankCapacityInLiters: vehicles[0].FuelTankCapacityInLiters,
                Year: vehicles[0].Year,
                VehicleGroupId: vehicles[0].VehicleGroupId,
                PhotoBytes: vehicles[0].PhotoBytes ?? Array.Empty<byte>()),
            new(
                Id: vehicles[1].Id,
                LicensePlate: vehicles[1].LicensePlate,
                Brand: vehicles[1].Brand,
                Model: vehicles[1].Model,
                Color: vehicles[1].Color,
                FuelType: vehicles[1].FuelType,
                FuelTankCapacityInLiters: vehicles[1].FuelTankCapacityInLiters,
                Year: vehicles[1].Year,
                VehicleGroupId: vehicles[1].VehicleGroupId,
                PhotoBytes: vehicles[1].PhotoBytes ?? Array.Empty<byte>())
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailVehicleDTO>>(vehicles))
            .Returns(expectedDtos);

        // act
        Result<VehiclesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(2, result.Value.Vehicles.Count);
        CollectionAssert.AreEquivalent(
            expectedDtos,
            result.Value.Vehicles.ToList());

        _vehicleRepositoryMock.Verify(r =>
            r.GetByVehicleGroupAsync(vehicleGroupId), Times.Once);
        _vehicleRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailVehicleDTO>>(vehicles), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InternalExceptionError_When_Exception_Occurs()
    {
        // arrange
        GetAllVehiclesQuery query = CreateValidQuery(vehicleGroupId: null, quantity: null);

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
            .Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<VehiclesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a listagem de veículos da empresa")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}