using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.DriverModule.Commands;
using OblivionDrive.Application.DriverModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.DriverModule.HandlerTests;

[TestClass]
[TestCategory("Driver - DeleteDriverHandler Unit Tests")]
public class DeleteDriverHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryDriver> _driverRepositoryMock = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;
    private Mock<IValidator<DeleteDriverCommand>> _validatorMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<DeleteDriverHandler>> _loggerMock = default!;
    private DeleteDriverHandler _handler = default!;

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
        _driverRepositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<Driver>()))
            .ReturnsAsync(true);

        _rentalRepositoryMock = new Mock<IRepositoryRental>();
        _rentalRepositoryMock
            .Setup(r => r.ExistsAnyRentalForDriverAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<DeleteDriverCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<DeleteDriverCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<DeleteDriverHandler>>();

        _handler = new DeleteDriverHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _driverRepositoryMock.Object,
            _rentalRepositoryMock.Object,
            _validatorMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private static DeleteDriverCommand CreateValidCommand()
        => new DeleteDriverCommand(Guid.NewGuid());

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
        => new User
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? userId
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
        DeleteDriverCommand command = CreateValidCommand();

        var failures = new List<ValidationFailure>
        {
            new(nameof(DeleteDriverCommand.DriverId), "O identificador do condutor é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _rentalRepositoryMock.Verify(r =>
            r.ExistsAnyRentalForDriverAsync(It.IsAny<Guid>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Driver>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        DeleteDriverCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _rentalRepositoryMock.Verify(r =>
            r.ExistsAnyRentalForDriverAsync(It.IsAny<Guid>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Driver>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        DeleteDriverCommand command = CreateValidCommand();

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

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _rentalRepositoryMock.Verify(r =>
            r.ExistsAnyRentalForDriverAsync(It.IsAny<Guid>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Driver>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Driver_Does_Not_Exist()
    {
        // arrange
        DeleteDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync((Driver?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.DriverId), Times.Once);

        _rentalRepositoryMock.Verify(r =>
            r.ExistsAnyRentalForDriverAsync(It.IsAny<Guid>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Driver_Belongs_To_Other_Company()
    {
        // arrange
        DeleteDriverCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync(driverFromOtherCompany);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.DriverId), Times.Once);

        _rentalRepositoryMock.Verify(r =>
            r.ExistsAnyRentalForDriverAsync(It.IsAny<Guid>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Driver_Has_Any_Rental()
    {
        // arrange
        DeleteDriverCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync(existingDriver);

        _rentalRepositoryMock
            .Setup(r => r.ExistsAnyRentalForDriverAsync(existingDriver.Id))
            .ReturnsAsync(true);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.DriverId), Times.Once);
        _rentalRepositoryMock.Verify(r =>
            r.ExistsAnyRentalForDriverAsync(existingDriver.Id), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Delete_Driver_And_Return_Success()
    {
        // arrange
        DeleteDriverCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync(existingDriver);

        _rentalRepositoryMock
            .Setup(r => r.ExistsAnyRentalForDriverAsync(existingDriver.Id))
            .ReturnsAsync(false);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.DriverId), Times.Once);
        _rentalRepositoryMock.Verify(r =>
            r.ExistsAnyRentalForDriverAsync(existingDriver.Id), Times.Once);
        _driverRepositoryMock.Verify(r =>
            r.DeleteAsync(existingDriver), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        DeleteDriverCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync(existingDriver);

        _rentalRepositoryMock
            .Setup(r => r.ExistsAnyRentalForDriverAsync(existingDriver.Id))
            .ReturnsAsync(false);

        _driverRepositoryMock
            .Setup(r => r.DeleteAsync(existingDriver))
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
                    v.ToString()!.Contains("Ocorreu um erro durante a exclusão de condutor")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}