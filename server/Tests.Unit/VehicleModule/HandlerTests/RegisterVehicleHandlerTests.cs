using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.VehicleModule.Commands;
using OblivionDrive.Application.VehicleModule.DTOs;
using OblivionDrive.Application.VehicleModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Tests.Unit.VehicleModule.HandlerTests;

[TestClass]
[TestCategory("Vehicle - RegisterVehicleHandler Unit Tests")]
public class RegisterVehicleHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryVehicle> _vehicleRepositoryMock = default!;
    private Mock<IRepositoryVehicleGroup> _vehicleGroupRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IValidator<RegisterVehicleCommand>> _validatorMock = default!;
    private Mock<ILogger<RegisterVehicleCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private RegisterVehicleHandler _handler = default!;

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

        _vehicleGroupRepositoryMock = new Mock<IRepositoryVehicleGroup>();

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<RegisterVehicleCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterVehicleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<RegisterVehicleCommand>>();

        _mapperMock = new Mock<IMapper>();

        _handler = new RegisterVehicleHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _vehicleRepositoryMock.Object,
            _vehicleGroupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    private static RegisterVehicleCommand CreateValidCommand()
    {
        return new RegisterVehicleCommand(
            LicensePlate: "ABC1D23",
            Brand: "Toyota",
            Model: "Corolla",
            Color: "White",
            FuelType: FuelType.Gasoline,
            FuelTankCapacityInLiters: 55.5m,
            Year: DateTime.UtcNow.Year,
            VehicleGroupId: Guid.NewGuid(),
            PhotoBytes: new byte[] { 1, 2, 3 }
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

    private static VehicleGroup CreateVehicleGroup(Guid companyId)
    {
        return new VehicleGroup(
            name: "Grupo de veículos teste",
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(RegisterVehicleCommand.LicensePlate), "A placa do veículo é obrigatória.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<VehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<VehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<VehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_VehicleGroup_Does_Not_Exist()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync((VehicleGroup?)null);

        // act
        Result<VehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleGroupId), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_VehicleGroup_Belongs_To_Other_Company()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        VehicleGroup otherCompanyVehicleGroup = CreateVehicleGroup(otherCompanyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(otherCompanyVehicleGroup);

        // act
        Result<VehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleGroupId), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Create_Vehicle_And_Return_Success()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(vehicleGroup);

        Vehicle? capturedVehicle = null;

        _vehicleRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Vehicle>()))
            .Callback<Vehicle>(v => capturedVehicle = v)
            .ReturnsAsync(Guid.NewGuid());

        VehicleDTO expectedDto = new(
            CreatedSuccessfully: true,
            LicensePlate: command.LicensePlate,
            Brand: command.Brand,
            Model: command.Model,
            Color: command.Color,
            FuelType: command.FuelType,
            FuelTankCapacityInLiters: command.FuelTankCapacityInLiters,
            Year: command.Year,
            VehicleGroupId: command.VehicleGroupId,
            PhotoBytes: command.PhotoBytes
        );

        _mapperMock
            .Setup(m => m.Map<VehicleDTO>(It.IsAny<Vehicle>()))
            .Returns(expectedDto);

        // act
        Result<VehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(capturedVehicle);
        Assert.AreNotEqual(Guid.Empty, capturedVehicle!.Id);
        Assert.AreEqual(companyId, capturedVehicle.CompanyId);
        Assert.AreEqual(command.LicensePlate, capturedVehicle.LicensePlate);
        Assert.AreEqual(command.Brand, capturedVehicle.Brand);
        Assert.AreEqual(command.Model, capturedVehicle.Model);
        Assert.AreEqual(command.Color, capturedVehicle.Color);
        Assert.AreEqual(command.FuelType, capturedVehicle.FuelType);
        Assert.AreEqual(command.FuelTankCapacityInLiters, capturedVehicle.FuelTankCapacityInLiters);
        Assert.AreEqual(command.Year, capturedVehicle.Year);
        Assert.AreEqual(command.VehicleGroupId, capturedVehicle.VehicleGroupId);
        CollectionAssert.AreEqual(command.PhotoBytes, capturedVehicle.PhotoBytes);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleGroupId), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Vehicle>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(vehicleGroup);

        _vehicleRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Vehicle>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<VehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante o registro de veículo")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}