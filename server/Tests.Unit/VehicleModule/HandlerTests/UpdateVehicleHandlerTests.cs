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
[TestCategory("Vehicle - UpdateVehicleHandler Unit Tests")]
public class UpdateVehicleHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryVehicle> _vehicleRepositoryMock = default!;
    private Mock<IRepositoryVehicleGroup> _vehicleGroupRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IValidator<UpdateVehicleCommand>> _validatorMock = default!;
    private Mock<ILogger<UpdateVehicleCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private UpdateVehicleHandler _handler = default!;

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

        _validatorMock = new Mock<IValidator<UpdateVehicleCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateVehicleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<UpdateVehicleCommand>>();

        _mapperMock = new Mock<IMapper>();

        _handler = new UpdateVehicleHandler(
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

    private static UpdateVehicleCommand CreateValidCommand(
    Guid? vehicleId = null,
    Guid? vehicleGroupId = null,
    byte[]? photoBytes = null)
    {
        return new UpdateVehicleCommand(
            VehicleId: vehicleId ?? Guid.NewGuid(),
            Brand: "Toyota",
            Model: "Corolla",
            Color: "White",
            FuelType: FuelType.Gasoline,
            FuelTankCapacityInLiters: 55.5m,
            Year: DateTime.UtcNow.Year,
            VehicleGroupId: vehicleGroupId ?? Guid.NewGuid(),
            PhotoBytes: photoBytes
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
            name: "Grupo de veículos",
            companyId: companyId);
    }

    private static Vehicle CreateVehicle(Guid companyId, Guid vehicleGroupId, byte[]? photoBytes = null)
    {
        var vehicle = new Vehicle(
            licensePlate: "OLD1234",
            brand: "Old Brand",
            model: "Old Model",
            color: "Black",
            fuelType: FuelType.Gasoline,
            fuelTankCapacityInLiters: 50m,
            year: DateTime.UtcNow.Year - 1,
            vehicleGroupId: vehicleGroupId,
            companyId: companyId);

        if (photoBytes is not null && photoBytes.Length > 0)
        {
            vehicle.SetPhoto(photoBytes);
        }

        return vehicle;
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(UpdateVehicleCommand.Brand), "A marca do veículo é obrigatória.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<UpdatedVehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<UpdatedVehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<UpdatedVehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Vehicle_Does_Not_Exist()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.VehicleId))
            .ReturnsAsync((Vehicle?)null);

        // act
        Result<UpdatedVehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleId), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Vehicle_Belongs_To_Other_Company()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Vehicle otherCompanyVehicle = CreateVehicle(otherCompanyId, command.VehicleGroupId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleId))
            .ReturnsAsync(otherCompanyVehicle);

        // act
        Result<UpdatedVehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleId), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_VehicleGroup_Does_Not_Exist()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Vehicle existingVehicle = CreateVehicle(companyId, command.VehicleGroupId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleId))
            .ReturnsAsync(existingVehicle);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync((VehicleGroup?)null);

        // act
        Result<UpdatedVehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleGroupId), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_VehicleGroup_Belongs_To_Other_Company()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Vehicle existingVehicle = CreateVehicle(companyId, command.VehicleGroupId);
        VehicleGroup otherCompanyVehicleGroup = CreateVehicleGroup(otherCompanyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleId))
            .ReturnsAsync(existingVehicle);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(otherCompanyVehicleGroup);

        // act
        Result<UpdatedVehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleGroupId), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Update_Vehicle_And_Return_Success_With_New_Photo()
    {
        // arrange
        byte[] newPhoto = { 7, 7, 7 };
        UpdateVehicleCommand command = CreateValidCommand(photoBytes: newPhoto);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = command.VehicleGroupId;

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        byte[] oldPhoto = { 1, 2, 3 };
        Vehicle existingVehicle = CreateVehicle(companyId, vehicleGroupId, oldPhoto);
        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleId))
            .ReturnsAsync(existingVehicle);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(vehicleGroup);

        Vehicle? capturedExisting = null;
        Vehicle? capturedUpdatedData = null;
        Vehicle updatedVehicle = existingVehicle;

        _vehicleRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<Vehicle>()))
            .Callback<Vehicle, Vehicle>((existing, updatedData) =>
            {
                capturedExisting = existing;
                capturedUpdatedData = updatedData;

                existing.Update(updatedData);
                updatedVehicle = existing;
            })
            .ReturnsAsync(() => updatedVehicle);

        UpdatedVehicleDTO expectedDto = new(
            UpdatedSuccessfully: true,
            LicensePlate: existingVehicle.LicensePlate,
            Brand: command.Brand,
            Model: command.Model,
            Color: command.Color,
            FuelType: command.FuelType,
            FuelTankCapacityInLiters: command.FuelTankCapacityInLiters,
            Year: command.Year,
            VehicleGroupId: command.VehicleGroupId,
            PhotoBytes: newPhoto
        );

        _mapperMock
            .Setup(m => m.Map<UpdatedVehicleDTO>(updatedVehicle))
            .Returns(expectedDto);

        // act
        Result<UpdatedVehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(capturedUpdatedData);
        Assert.AreEqual(command.Brand, capturedUpdatedData!.Brand);
        Assert.AreEqual(command.Model, capturedUpdatedData.Model);
        Assert.AreEqual(command.Color, capturedUpdatedData.Color);
        Assert.AreEqual(command.FuelType, capturedUpdatedData.FuelType);
        Assert.AreEqual(command.FuelTankCapacityInLiters, capturedUpdatedData.FuelTankCapacityInLiters);
        Assert.AreEqual(command.Year, capturedUpdatedData.Year);
        Assert.AreEqual(command.VehicleGroupId, capturedUpdatedData.VehicleGroupId);
        CollectionAssert.AreEqual(newPhoto, capturedUpdatedData.PhotoBytes);

        Assert.IsNotNull(capturedExisting);
        Assert.AreEqual(existingVehicle.Id, capturedExisting!.Id);
        Assert.AreEqual(companyId, capturedExisting.CompanyId);
        Assert.AreEqual(existingVehicle.LicensePlate, capturedExisting.LicensePlate);
        CollectionAssert.AreEqual(newPhoto, capturedExisting.PhotoBytes);

        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleId), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleGroupId), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<Vehicle>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Keep_Existing_Photo_When_Command_Photo_Is_Null_Or_Empty()
    {
        // arrange
        byte[] existingPhoto = { 1, 2, 3 };
        UpdateVehicleCommand command = CreateValidCommand(photoBytes: null);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = command.VehicleGroupId;

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Vehicle existingVehicle = CreateVehicle(companyId, vehicleGroupId, existingPhoto);
        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleId))
            .ReturnsAsync(existingVehicle);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(vehicleGroup);

        Vehicle? capturedUpdatedData = null;
        Vehicle updatedVehicle = existingVehicle;

        _vehicleRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<Vehicle>()))
            .Callback<Vehicle, Vehicle>((existing, updatedData) =>
            {
                capturedUpdatedData = updatedData;
                existing.Update(updatedData);
                updatedVehicle = existing;
            })
            .ReturnsAsync(() => updatedVehicle);

        UpdatedVehicleDTO expectedDto = new(
            UpdatedSuccessfully: true,
            LicensePlate: existingVehicle.LicensePlate,
            Brand: command.Brand,
            Model: command.Model,
            Color: command.Color,
            FuelType: command.FuelType,
            FuelTankCapacityInLiters: command.FuelTankCapacityInLiters,
            Year: command.Year,
            VehicleGroupId: command.VehicleGroupId,
            PhotoBytes: existingPhoto
        );

        _mapperMock
            .Setup(m => m.Map<UpdatedVehicleDTO>(updatedVehicle))
            .Returns(expectedDto);

        // act
        Result<UpdatedVehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(capturedUpdatedData);
        CollectionAssert.AreEqual(existingPhoto, capturedUpdatedData!.PhotoBytes);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = command.VehicleGroupId;

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Vehicle existingVehicle = CreateVehicle(companyId, vehicleGroupId);
        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleId))
            .ReturnsAsync(existingVehicle);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(vehicleGroup);

        _vehicleRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<Vehicle>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<UpdatedVehicleDTO> result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante a atualização do veículo")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}