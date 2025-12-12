using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.ClientModule.DTOs;
using OblivionDrive.Application.ClientModule.Handlers;
using OblivionDrive.Application.ClientModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ClientModule;

namespace OblivionDrive.Tests.Unit.ClientModule.HandlerTests;

[TestClass]
[TestCategory("Client - GetClientByIdHandler Unit Tests")]
public class GetClientByIdHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryClient> _clientRepositoryMock = default!;
    private Mock<IValidator<GetClientByIdQuery>> _validatorMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetClientByIdHandler>> _loggerMock = default!;
    private GetClientByIdHandler _handler = default!;

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

        _validatorMock = new Mock<IValidator<GetClientByIdQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetClientByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<DetailClientDTO>(It.IsAny<Client>()))
            .Returns(default(DetailClientDTO)!);

        _loggerMock = new Mock<ILogger<GetClientByIdHandler>>();

        _handler = new GetClientByIdHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _clientRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetClientByIdQuery CreateValidQuery()
    {
        return new GetClientByIdQuery(
            ClientId: Guid.NewGuid()
        );
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
        GetClientByIdQuery query = CreateValidQuery();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetClientByIdQuery.ClientId), "O identificador do cliente é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<DetailClientDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailClientDTO>(It.IsAny<Client>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetClientByIdQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<DetailClientDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailClientDTO>(It.IsAny<Client>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetClientByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<DetailClientDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailClientDTO>(It.IsAny<Client>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Client_Is_Not_Found()
    {
        // arrange
        GetClientByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(query.ClientId))
            .ReturnsAsync((Client?)null);

        // act
        Result<DetailClientDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.ClientId), Times.Once);
        _mapperMock.Verify(m =>
            m.Map<DetailClientDTO>(It.IsAny<Client>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Client_Belongs_To_Other_Company()
    {
        // arrange
        GetClientByIdQuery query = CreateValidQuery();

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
        Client clientFromOtherCompany = CreateClient(query.ClientId, otherCompanyId, "Cliente de outra empresa");

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(query.ClientId))
            .ReturnsAsync(clientFromOtherCompany);

        // act
        Result<DetailClientDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        Assert.AreNotEqual(currentCompanyId, clientFromOtherCompany.CompanyId);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.ClientId), Times.Once);
        _mapperMock.Verify(m =>
            m.Map<DetailClientDTO>(It.IsAny<Client>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InternalException_When_Exception_Occurs()
    {
        // arrange
        GetClientByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(query.ClientId))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<DetailClientDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a obtenção de detalhes do cliente")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailClientDTO>(It.IsAny<Client>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Client_Detail_When_Request_Is_Valid()
    {
        // arrange
        GetClientByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client client = CreateClient(query.ClientId, currentCompanyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(query.ClientId))
            .ReturnsAsync(client);

        var expectedDto = new DetailClientDTO
        (
            Id: client.Id,
            Name: client.Name,
            Email: client.Email,
            PhoneNumber: client.PhoneNumber,
            ClientType: client.ClientType,
            Cpf: client.Cpf,
            Rg: client.Rg,
            Cnh: client.Cnh,
            Cnpj: client.Cnpj,
            State: client.Address.State,
            City: client.Address.City,
            District: client.Address.District,
            Street: client.Address.Street,
            Number: client.Address.Number
        );

        _mapperMock
            .Setup(m => m.Map<DetailClientDTO>(client))
            .Returns(expectedDto);

        // act
        Result<DetailClientDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);
        Assert.AreEqual(client.Id, result.Value.Id);
        Assert.AreEqual(client.Name, result.Value.Name);
        Assert.AreEqual(client.Email, result.Value.Email);
        Assert.AreEqual(client.PhoneNumber, result.Value.PhoneNumber);
        Assert.AreEqual(client.ClientType, result.Value.ClientType);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.ClientId), Times.Once);
        _mapperMock.Verify(m =>
            m.Map<DetailClientDTO>(client), Times.Once);
    }
}
