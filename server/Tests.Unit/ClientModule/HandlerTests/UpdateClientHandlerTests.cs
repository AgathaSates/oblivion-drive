using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.ClientModule.Commands;
using OblivionDrive.Application.ClientModule.DTOs;
using OblivionDrive.Application.ClientModule.Handlers;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.ClientModule.HandlerTests;

[TestClass]
[TestCategory("Client - UpdateClientHandler Unit Tests")]
public class UpdateClientHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryClient> _clientRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IValidator<UpdateClientCommand>> _validatorMock = default!;
    private Mock<ILogger<UpdateClientCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private UpdateClientHandler _handler = default!;

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
            .Setup(r => r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()))
            .ReturnsAsync((Client existing, Client updated) =>
            {
                existing.Update(updated);
                return existing;
            });

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCpfAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByRgAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCnhAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCnpjAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<UpdateClientCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateClientCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<UpdateClientCommand>>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<UpdatedClientDTO>(It.IsAny<Client>()))
            .Returns(default(UpdatedClientDTO)!);

        _handler = new UpdateClientHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _clientRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    private static UpdateClientCommand CreateValidIndividualCommand(Guid clientId)
    {
        return new UpdateClientCommand(
            ClientId: clientId,
            Name: "João da Silva Atualizado",
            Email: "joao.silva.updated@example.com",
            PhoneNumber: "11999887766",
            ClientType: ClientType.Individual,
            Cpf: "98765432100",
            Rg: "987654321",
            Cnh: "98765432100",
            Cnpj: null,
            State: "Rio de Janeiro",
            City: "Rio de Janeiro",
            District: "Copacabana",
            Street: "Avenida Atlântica",
            Number: "789"
        );
    }

    private static UpdateClientCommand CreateValidLegalEntityCommand(Guid clientId)
    {
        return new UpdateClientCommand(
            ClientId: clientId,
            Name: "Empresa XYZ S.A.",
            Email: "contato@empresaxyz.com",
            PhoneNumber: "3144445555",
            ClientType: ClientType.LegalEntity,
            Cpf: null,
            Rg: null,
            Cnh: null,
            Cnpj: "98765432000188",
            State: "Minas Gerais",
            City: "Belo Horizonte",
            District: "Savassi",
            Street: "Avenida Getúlio Vargas",
            Number: "200"
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

    private static Client CreateExistingIndividualClient(Guid clientId, Guid companyId)
    {
        Address address = new(
            state: "São Paulo",
            city: "São Paulo",
            district: "Centro",
            street: "Rua das Flores",
            number: "123"
        );

        var client = new Client(
            companyId: companyId,
            name: "João da Silva",
            email: "joao.silva@example.com",
            phoneNumber: "11987654321",
            clientType: ClientType.Individual,
            address: address,
            cpf: "12345678901",
            rg: "123456789",
            cnh: "12345678901"
        );

        client.Id = clientId;
        return client;
    }

    private static Client CreateExistingLegalEntityClient(Guid clientId, Guid companyId)
    {
        Address address = new(
            state: "São Paulo",
            city: "São Paulo",
            district: "Centro",
            street: "Rua Comercial",
            number: "100"
        );

        var client = new Client(
            companyId: companyId,
            name: "Empresa ABC Ltda",
            email: "contato@empresaabc.com",
            phoneNumber: "1133334444",
            clientType: ClientType.LegalEntity,
            address: address,
            cnpj: "12345678000199"
        );

        client.Id = clientId;
        return client;
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidIndividualCommand(clientId);

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(UpdateClientCommand.Name), "O nome do cliente é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidIndividualCommand(clientId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidIndividualCommand(clientId);

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Client_Does_Not_Exist()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidIndividualCommand(clientId);

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(clientId))
            .ReturnsAsync((Client?)null);

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(clientId), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Client_Belongs_To_Different_Company()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidIndividualCommand(clientId);

        Guid currentUserId = Guid.NewGuid();
        Guid differentCompanyId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client existingClient = CreateExistingIndividualClient(clientId, differentCompanyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(clientId))
            .ReturnsAsync(existingClient);

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(clientId), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Email_Already_Exists()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidIndividualCommand(clientId);

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid companyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client existingClient = CreateExistingIndividualClient(clientId, companyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(clientId))
            .ReturnsAsync(existingClient);

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email, clientId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        var error = result.Errors.Single();
        Assert.IsTrue(
            error.Reasons.Any(r =>
                r.Message.Contains("Já existe um cliente cadastrado com este e-mail", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um cliente cadastrado com este e-mail.");

        _clientRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(command.Email, clientId), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_PhoneNumber_Already_Exists()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidIndividualCommand(clientId);

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid companyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client existingClient = CreateExistingIndividualClient(clientId, companyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(clientId))
            .ReturnsAsync(existingClient);

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email, clientId))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(command.PhoneNumber, clientId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        var error = result.Errors.Single();
        Assert.IsTrue(
            error.Reasons.Any(r =>
                r.Message.Contains("Já existe um cliente cadastrado com este telefone", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um cliente cadastrado com este telefone.");

        _clientRepositoryMock.Verify(r =>
            r.ExistsByPhoneNumberAsync(command.PhoneNumber, clientId), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Cpf_Already_Exists()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidIndividualCommand(clientId);

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid companyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client existingClient = CreateExistingIndividualClient(clientId, companyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(clientId))
            .ReturnsAsync(existingClient);

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email, clientId))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(command.PhoneNumber, clientId))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCpfAsync(command.Cpf!, clientId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        var error = result.Errors.Single();
        Assert.IsTrue(
            error.Reasons.Any(r =>
                r.Message.Contains("Já existe um cliente cadastrado com este CPF", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um cliente cadastrado com este CPF.");

        _clientRepositoryMock.Verify(r =>
            r.ExistsByCpfAsync(command.Cpf!, clientId), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Rg_Already_Exists()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidIndividualCommand(clientId);

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid companyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client existingClient = CreateExistingIndividualClient(clientId, companyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(clientId))
            .ReturnsAsync(existingClient);

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email, clientId))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(command.PhoneNumber, clientId))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCpfAsync(command.Cpf!, clientId))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByRgAsync(command.Rg!, clientId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        var error = result.Errors.Single();
        Assert.IsTrue(
            error.Reasons.Any(r =>
                r.Message.Contains("Já existe um cliente cadastrado com este RG", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um cliente cadastrado com este RG.");

        _clientRepositoryMock.Verify(r =>
            r.ExistsByRgAsync(command.Rg!, clientId), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Cnh_Already_Exists()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidIndividualCommand(clientId);

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid companyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client existingClient = CreateExistingIndividualClient(clientId, companyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(clientId))
            .ReturnsAsync(existingClient);

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email, clientId))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(command.PhoneNumber, clientId))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCpfAsync(command.Cpf!, clientId))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByRgAsync(command.Rg!, clientId))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCnhAsync(command.Cnh!, clientId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        var error = result.Errors.Single();
        Assert.IsTrue(
            error.Reasons.Any(r =>
                r.Message.Contains("Já existe um cliente cadastrado com esta CNH", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um cliente cadastrado com esta CNH.");

        _clientRepositoryMock.Verify(r =>
            r.ExistsByCnhAsync(command.Cnh!, clientId), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Cnpj_Already_Exists()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidLegalEntityCommand(clientId);

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid companyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client existingClient = CreateExistingLegalEntityClient(clientId, companyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(clientId))
            .ReturnsAsync(existingClient);

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email, clientId))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(command.PhoneNumber, clientId))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCnpjAsync(command.Cnpj!, clientId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        var error = result.Errors.Single();
        Assert.IsTrue(
            error.Reasons.Any(r =>
                r.Message.Contains("Já existe um cliente cadastrado com este CNPJ", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um cliente cadastrado com este CNPJ.");

        _clientRepositoryMock.Verify(r =>
            r.ExistsByCnpjAsync(command.Cnpj!, clientId), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Update_Individual_Client_And_Return_Success()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidIndividualCommand(clientId);

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid companyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client existingClient = CreateExistingIndividualClient(clientId, companyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(clientId))
            .ReturnsAsync(existingClient);

        Client? updatedClient = null;

        _clientRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()))
            .Callback<Client, Client>((existing, updated) =>
            {
                existing.Update(updated);
                updatedClient = existing;
            })
            .ReturnsAsync((Client existing, Client updated) =>
            {
                existing.Update(updated);
                return existing;
            });

        var expectedDto = new UpdatedClientDTO(
            UpdatedSuccessfully: true,
            Name: NameFormatter.FormatName(command.Name),
            Email: command.Email,
            PhoneNumber: command.PhoneNumber,
            ClientType: ClientType.Individual,
            Cpf: command.Cpf,
            Rg: command.Rg,
            Cnh: command.Cnh,
            Cnpj: null,
            State: command.State,
            City: command.City,
            District: command.District,
            Street: command.Street,
            Number: command.Number
        );

        _mapperMock
            .Setup(m => m.Map<UpdatedClientDTO>(It.IsAny<Client>()))
            .Returns(expectedDto);

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(updatedClient);
        Assert.AreEqual(clientId, updatedClient!.Id);
        Assert.AreEqual(companyId, updatedClient.CompanyId);
        Assert.AreEqual(NameFormatter.FormatName(command.Name), updatedClient.Name);
        Assert.AreEqual(command.Email, updatedClient.Email);
        Assert.AreEqual(command.PhoneNumber, updatedClient.PhoneNumber);
        Assert.AreEqual(ClientType.Individual, updatedClient.ClientType);
        Assert.AreEqual(command.Cpf, updatedClient.Cpf);
        Assert.AreEqual(command.Rg, updatedClient.Rg);
        Assert.AreEqual(command.Cnh, updatedClient.Cnh);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(clientId), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(command.Email, clientId), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.ExistsByPhoneNumberAsync(command.PhoneNumber, clientId), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.ExistsByCpfAsync(command.Cpf!, clientId), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.ExistsByRgAsync(command.Rg!, clientId), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.ExistsByCnhAsync(command.Cnh!, clientId), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Update_LegalEntity_Client_And_Return_Success()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidLegalEntityCommand(clientId);

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid companyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client existingClient = CreateExistingLegalEntityClient(clientId, companyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(clientId))
            .ReturnsAsync(existingClient);

        Client? updatedClient = null;

        _clientRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()))
            .Callback<Client, Client>((existing, updated) =>
            {
                existing.Update(updated);
                updatedClient = existing;
            })
            .ReturnsAsync((Client existing, Client updated) =>
            {
                existing.Update(updated);
                return existing;
            });

        var expectedDto = new UpdatedClientDTO(
            UpdatedSuccessfully: true,
            Name: NameFormatter.FormatName(command.Name),
            Email: command.Email,
            PhoneNumber: command.PhoneNumber,
            ClientType: ClientType.LegalEntity,
            Cpf: null,
            Rg: null,
            Cnh: null,
            Cnpj: command.Cnpj,
            State: command.State,
            City: command.City,
            District: command.District,
            Street: command.Street,
            Number: command.Number
        );

        _mapperMock
            .Setup(m => m.Map<UpdatedClientDTO>(It.IsAny<Client>()))
            .Returns(expectedDto);

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(updatedClient);
        Assert.AreEqual(clientId, updatedClient!.Id);
        Assert.AreEqual(companyId, updatedClient.CompanyId);
        Assert.AreEqual(NameFormatter.FormatName(command.Name), updatedClient.Name);
        Assert.AreEqual(command.Email, updatedClient.Email);
        Assert.AreEqual(command.PhoneNumber, updatedClient.PhoneNumber);
        Assert.AreEqual(ClientType.LegalEntity, updatedClient.ClientType);
        Assert.AreEqual(command.Cnpj, updatedClient.Cnpj);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(clientId), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(command.Email, clientId), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.ExistsByPhoneNumberAsync(command.PhoneNumber, clientId), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.ExistsByCnpjAsync(command.Cnpj!, clientId), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        Guid clientId = Guid.NewGuid();
        UpdateClientCommand command = CreateValidIndividualCommand(clientId);

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid companyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client existingClient = CreateExistingIndividualClient(clientId, companyId);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(clientId))
            .ReturnsAsync(existingClient);

        _clientRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Client>(), It.IsAny<Client>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<UpdatedClientDTO> result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante a atualização do cliente")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
