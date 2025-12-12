using System.Collections.Immutable;
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
[TestCategory("Client - GetAllClientsHandler Unit Tests")]
public class GetAllClientsHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryClient> _clientRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetAllClientsHandler>> _loggerMock = default!;
    private Mock<IValidator<GetAllClientsQuery>> _validatorMock = default!;
    private GetAllClientsHandler _handler = default!;

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

        _mapperMock = new Mock<IMapper>();

        _loggerMock = new Mock<ILogger<GetAllClientsHandler>>();

        _validatorMock = new Mock<IValidator<GetAllClientsQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetAllClientsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetAllClientsHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _clientRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetAllClientsQuery CreateQuery(int? quantity = null)
    {
        return new GetAllClientsQuery(quantity);
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

    private static Client CreateClient(Guid clientId, Guid companyId, string name)
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
            email: $"{name.Replace(" ", "").ToLower()}@example.com",
            cpf: "12345678901",
            rg: "123456789",
            cnh: "12345678901"
        );
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetAllClientsQuery query = CreateQuery();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetAllClientsQuery.Quantity), "A quantidade deve ser maior que zero.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<ClientsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailClientDTO>>(It.IsAny<IReadOnlyCollection<Client>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetAllClientsQuery query = CreateQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<ClientsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailClientDTO>>(It.IsAny<IReadOnlyCollection<Client>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetAllClientsQuery query = CreateQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<ClientsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailClientDTO>>(It.IsAny<IReadOnlyCollection<Client>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InternalException_When_Repository_Throws()
    {
        // arrange
        GetAllClientsQuery query = CreateQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("Erro ao listar clientes"));

        // act
        Result<ClientsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a listagem de clientes da empresa")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mapperMock.Verify(m =>
            m.Map<List<DetailClientDTO>>(It.IsAny<IReadOnlyCollection<Client>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_All_Clients_When_Quantity_Is_Null()
    {
        // arrange
        GetAllClientsQuery query = CreateQuery(quantity: null);

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var clients = new List<Client>
        {
            CreateClient(Guid.NewGuid(), currentCompanyId, "João da Silva"),
            CreateClient(Guid.NewGuid(), currentCompanyId, "Maria Souza")
        };

        _clientRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(clients);

        var clientDtos = new List<DetailClientDTO>
        {
            new(
                Id: clients[0].Id,
                Name: clients[0].Name,
                Email: clients[0].Email,
                PhoneNumber: clients[0].PhoneNumber,
                ClientType: clients[0].ClientType,
                Cpf: clients[0].Cpf,
                Rg: clients[0].Rg,
                Cnh: clients[0].Cnh,
                Cnpj: clients[0].Cnpj,
                State: clients[0].Address.State,
                City: clients[0].Address.City,
                District: clients[0].Address.District,
                Street: clients[0].Address.Street,
                Number: clients[0].Address.Number
            ),
            new(
                Id: clients[1].Id,
                Name: clients[1].Name,
                Email: clients[1].Email,
                PhoneNumber: clients[1].PhoneNumber,
                ClientType: clients[1].ClientType,
                Cpf: clients[1].Cpf,
                Rg: clients[1].Rg,
                Cnh: clients[1].Cnh,
                Cnpj: clients[1].Cnpj,
                State: clients[1].Address.State,
                City: clients[1].Address.City,
                District: clients[1].Address.District,
                Street: clients[1].Address.Street,
                Number: clients[1].Address.Number
            )
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailClientDTO>>(clients))
            .Returns(clientDtos);

        // act
        Result<ClientsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);

        ImmutableList<DetailClientDTO> returnedClients = result.Value.Clients;
        Assert.AreEqual(clientDtos.Count, returnedClients.Count);
        CollectionAssert.AreEqual(clientDtos, returnedClients.ToList());

        _clientRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailClientDTO>>(clients), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Limited_Clients_When_Quantity_Is_Specified()
    {
        // arrange
        const int quantity = 1;
        GetAllClientsQuery query = CreateQuery(quantity);

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var clients = new List<Client>
        {
            CreateClient(Guid.NewGuid(), currentCompanyId, "João da Silva")
        };

        _clientRepositoryMock
            .Setup(r => r.GetAllAsync(quantity))
            .ReturnsAsync(clients);

        var clientDtos = new List<DetailClientDTO>
        {
            new(
                Id: clients[0].Id,
                Name: clients[0].Name,
                Email: clients[0].Email,
                PhoneNumber: clients[0].PhoneNumber,
                ClientType: clients[0].ClientType,
                Cpf: clients[0].Cpf,
                Rg: clients[0].Rg,
                Cnh: clients[0].Cnh,
                Cnpj: clients[0].Cnpj,
                State: clients[0].Address.State,
                City: clients[0].Address.City,
                District: clients[0].Address.District,
                Street: clients[0].Address.Street,
                Number: clients[0].Address.Number
            )
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailClientDTO>>(clients))
            .Returns(clientDtos);

        // act
        Result<ClientsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);

        ImmutableList<DetailClientDTO> returnedClients = result.Value.Clients;
        Assert.AreEqual(1, returnedClients.Count);
        CollectionAssert.AreEqual(clientDtos, returnedClients.ToList());

        _clientRepositoryMock.Verify(r =>
            r.GetAllAsync(quantity), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailClientDTO>>(clients), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Empty_List_When_No_Clients_Exist()
    {
        // arrange
        GetAllClientsQuery query = CreateQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var emptyClients = new List<Client>();

        _clientRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(emptyClients);

        var emptyClientDtos = new List<DetailClientDTO>();

        _mapperMock
            .Setup(m => m.Map<List<DetailClientDTO>>(emptyClients))
            .Returns(emptyClientDtos);

        // act
        Result<ClientsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(0, result.Value.Clients.Count);

        _clientRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);
        _mapperMock.Verify(m =>
            m.Map<List<DetailClientDTO>>(emptyClients), Times.Once);
    }
}
