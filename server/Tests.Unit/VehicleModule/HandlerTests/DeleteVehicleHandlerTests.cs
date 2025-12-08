using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.VehicleModule.Commands;
using OblivionDrive.Application.VehicleModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Tests.Unit.VehicleModule.HandlerTests;

[TestClass]
[TestCategory("Vehicle - DeleteVehicleHandler Unit Tests")]
public class DeleteVehicleHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryVehicle> _vehicleRepositoryMock = default!;
    private Mock<IValidator<DeleteVehicleCommand>> _validatorMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<DeleteVehicleHandler>> _loggerMock = default!;
    private DeleteVehicleHandler _handler = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;

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

        _rentalRepositoryMock = new Mock<IRepositoryRental>();

        _validatorMock = new Mock<IValidator<DeleteVehicleCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<DeleteVehicleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<DeleteVehicleHandler>>();

        _handler = new DeleteVehicleHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _vehicleRepositoryMock.Object,
            _rentalRepositoryMock.Object,
            _validatorMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private static DeleteVehicleCommand CreateValidCommand(Guid? vehicleId = null)
    {
        return new DeleteVehicleCommand(
            VehicleId: vehicleId ?? Guid.NewGuid()
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

        return new Vehicle(
            licensePlate: "ABC1D23",
            brand: "Toyota",
            model: "Corolla",
            color: "White",
            fuelType: FuelType.Gasoline,
            fuelTankCapacityInLiters: 55.5m,
            year: DateTime.UtcNow.Year,
            vehicleGroupId: vehicleGroupId,
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        DeleteVehicleCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(DeleteVehicleCommand.VehicleId), "O identificador do veículo é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        DeleteVehicleCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        DeleteVehicleCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Vehicle_Does_Not_Exist()
    {
        // arrange
        DeleteVehicleCommand command = CreateValidCommand();

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
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleId), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Vehicle_Belongs_To_Other_Company()
    {
        // arrange
        DeleteVehicleCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.VehicleId))
            .ReturnsAsync(otherCompanyVehicle);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleId), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Vehicle>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Delete_Vehicle_And_Return_Success()
    {
        // arrange
        DeleteVehicleCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.VehicleId))
            .ReturnsAsync(existingVehicle);

        _vehicleRepositoryMock
            .Setup(r => r.DeleteAsync(existingVehicle))
            .ReturnsAsync(true);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        _vehicleRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleId), Times.Once);

        _vehicleRepositoryMock.Verify(r =>
            r.DeleteAsync(existingVehicle), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        DeleteVehicleCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.VehicleId))
            .ReturnsAsync(existingVehicle);

        _vehicleRepositoryMock
            .Setup(r => r.DeleteAsync(existingVehicle))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante a exclusão de veículo")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}