using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.ClientModule.Commands;
using OblivionDrive.Application.ClientModule.Handlers;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.ClientModule.HandlerTests;

[TestClass]
[TestCategory("Client - DeleteClientHandler Unit Tests")]
public class DeleteClientHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryClient> _clientRepositoryMock = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;
    private Mock<IValidator<DeleteClientCommand>> _validatorMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<DeleteClientHandler>> _loggerMock = default!;
    private DeleteClientHandler _handler = default!;

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

        _clientRepositoryMock = new Mock<IRepositoryClient>();
        _clientRepositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<Client>()))
            .ReturnsAsync(true);

        _rentalRepositoryMock = new Mock<IRepositoryRental>();
        _rentalRepositoryMock
            .Setup(r => r.ExistsOpenRentalForClientAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _validatorMock = new Mock<IValidator<DeleteClientCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<DeleteClientCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<DeleteClientHandler>>();

        _handler = new DeleteClientHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _clientRepositoryMock.Object,
            _rentalRepositoryMock.Object,
            _validatorMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private static DeleteClientCommand CreateValidCommand()
    {
        return new DeleteClientCommand(
            ClientId: Guid.NewGuid()
        );
    }

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
    {
        Guid effectiveCompanyId = companyId ?? userId;

        return new User
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = effectiveCompanyId
        };
    }

    private static Client CreateClient(Guid clientId, Guid companyId, string name = "João da Silva")
    {
        Address address = new Address(
            state: "São Paulo",
            city: "São Paulo",
            district: "Centro",
            street: "Rua das Flores",
            number: "123"
        );

        return new Client(
            companyId: companyId,
            name: name,
            phoneNumber: "11987654321",
            clientType: ClientType.Individual,
            address: address,
            email: "joao.silva@example.com",
            cpf: "12345678901",
            rg: "123456789",
            cnh: "12345678901"
        );
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        DeleteClientCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(DeleteClientCommand.ClientId),
                "O identificador do cliente é obrigatório.")
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
        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _rentalRepositoryMock.Verify(r =>
            r.ExistsOpenRentalForClientAsync(It.IsAny<Guid>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        DeleteClientCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _rentalRepositoryMock.Verify(r =>
            r.ExistsOpenRentalForClientAsync(It.IsAny<Guid>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        DeleteClientCommand command = CreateValidCommand();

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

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _rentalRepositoryMock.Verify(r =>
            r.ExistsOpenRentalForClientAsync(It.IsAny<Guid>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Client_Is_Not_Found()
    {
        // arrange
        DeleteClientCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync((Client?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ClientId), Times.Once);
        _rentalRepositoryMock.Verify(r =>
            r.ExistsOpenRentalForClientAsync(It.IsAny<Guid>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Client_Belongs_To_Other_Company()
    {
        // arrange
        DeleteClientCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Guid otherCompanyId = Guid.NewGuid();
        Client clientFromOtherCompany = CreateClient(
            command.ClientId,
            otherCompanyId,
            "Cliente de outra empresa"
        );

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(clientFromOtherCompany);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        Assert.AreNotEqual(currentCompanyId, clientFromOtherCompany.CompanyId);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ClientId), Times.Once);
        _rentalRepositoryMock.Verify(r =>
            r.ExistsOpenRentalForClientAsync(It.IsAny<Guid>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Client_Has_Open_Rental()
    {
        // arrange
        DeleteClientCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client client = CreateClient(command.ClientId, currentCompanyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _rentalRepositoryMock
            .Setup(r => r.ExistsOpenRentalForClientAsync(client.Id))
            .ReturnsAsync(true);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ClientId), Times.Once);
        _rentalRepositoryMock.Verify(r =>
            r.ExistsOpenRentalForClientAsync(client.Id), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Delete_Client_And_Commit_When_Request_Is_Valid()
    {
        // arrange
        DeleteClientCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client client = CreateClient(command.ClientId, currentCompanyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _rentalRepositoryMock
            .Setup(r => r.ExistsOpenRentalForClientAsync(client.Id))
            .ReturnsAsync(false);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ClientId), Times.Once);
        _rentalRepositoryMock.Verify(r =>
            r.ExistsOpenRentalForClientAsync(client.Id), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.DeleteAsync(client), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        DeleteClientCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client client = CreateClient(command.ClientId, currentCompanyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _rentalRepositoryMock
            .Setup(r => r.ExistsOpenRentalForClientAsync(client.Id))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.DeleteAsync(client))
            .ThrowsAsync(new Exception("Erro de banco ao excluir cliente"));

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
                    v.ToString()!.Contains("Ocorreu um erro durante a exclusão de cliente")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
