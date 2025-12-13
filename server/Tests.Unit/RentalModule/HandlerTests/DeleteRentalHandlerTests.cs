using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.RentalModule.Commands;
using OblivionDrive.Application.RentalModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.RentalModule.HandlerTests;

[TestClass]
[TestCategory("Rental - DeleteRentalHandler Unit Tests")]
public class DeleteRentalHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;
    private Mock<IValidator<DeleteRentalCommand>> _validatorMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<DeleteRentalHandler>> _loggerMock = default!;
    private DeleteRentalHandler _handler = default!;

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

        _rentalRepositoryMock = new Mock<IRepositoryRental>();
        _rentalRepositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<Rental>()))
            .ReturnsAsync(true);

        _validatorMock = new Mock<IValidator<DeleteRentalCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<DeleteRentalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<DeleteRentalHandler>>();

        _handler = new DeleteRentalHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _rentalRepositoryMock.Object,
            _validatorMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private static DeleteRentalCommand CreateValidCommand()
        => new(Guid.NewGuid());

    private static User CreateCompanyUser(Guid id, Guid? companyId = null)
        => new()
        {
            Id = id,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? id
        };

    private static Rental CreateRental(Guid companyId, bool isCompleted)
    {

        var rental = (Rental)Activator.CreateInstance(typeof(Rental), nonPublic: true)!;

        typeof(Rental).GetProperty(nameof(Rental.CompanyId))!.SetValue(rental, companyId);
        typeof(Rental).GetProperty(nameof(Rental.IsCompleted))!.SetValue(rental, isCompleted);
        typeof(Rental).GetProperty(nameof(Rental.Id))!.SetValue(rental, Guid.NewGuid());

        return rental;
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        DeleteRentalCommand command = CreateValidCommand();

        var failures = new List<ValidationFailure>
        {
            new(nameof(DeleteRentalCommand.RentalId), "O identificador do aluguel é obrigatório.")
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
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Rental>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        DeleteRentalCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Rental>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        DeleteRentalCommand command = CreateValidCommand();

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

        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Rental>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Rental_Does_Not_Exist()
    {
        // arrange
        DeleteRentalCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _rentalRepositoryMock
            .Setup(r => r.GetByIdAsync(command.RentalId))
            .ReturnsAsync((Rental?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.RentalId), Times.Once);

        _rentalRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Rental>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Rental_Belongs_To_Other_Company()
    {
        // arrange
        DeleteRentalCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Rental rentalFromOtherCompany = CreateRental(otherCompanyId, isCompleted: true);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _rentalRepositoryMock
            .Setup(r => r.GetByIdAsync(command.RentalId))
            .ReturnsAsync(rentalFromOtherCompany);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Rental>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Rental_Is_Not_Completed()
    {
        // arrange
        DeleteRentalCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Rental openRental = CreateRental(companyId, isCompleted: false);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _rentalRepositoryMock
            .Setup(r => r.GetByIdAsync(command.RentalId))
            .ReturnsAsync(openRental);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Rental>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Delete_Rental_And_Return_Success_When_Rental_Is_Completed_And_Belongs_To_Company()
    {
        // arrange
        DeleteRentalCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Rental completedRental = CreateRental(companyId, isCompleted: true);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _rentalRepositoryMock
            .Setup(r => r.GetByIdAsync(command.RentalId))
            .ReturnsAsync(completedRental);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        _rentalRepositoryMock.Verify(r =>
            r.DeleteAsync(completedRental), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        DeleteRentalCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Rental completedRental = CreateRental(companyId, isCompleted: true);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _rentalRepositoryMock
            .Setup(r => r.GetByIdAsync(command.RentalId))
            .ReturnsAsync(completedRental);

        _rentalRepositoryMock
            .Setup(r => r.DeleteAsync(completedRental))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a exclusão de aluguel")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
