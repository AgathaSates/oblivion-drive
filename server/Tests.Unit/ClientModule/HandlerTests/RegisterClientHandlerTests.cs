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
[TestCategory("Client - RegisterClientHandler Unit Tests")]
public class RegisterClientHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryClient> _clientRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IValidator<RegisterClientCommand>> _validatorMock = default!;
    private Mock<ILogger<RegisterClientCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private RegisterClientHandler _handler = default!;

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
            .Setup(r => r.AddAsync(It.IsAny<Client>()))
            .ReturnsAsync(Guid.NewGuid());

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCpfAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByRgAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCnhAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCnpjAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<RegisterClientCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterClientCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<RegisterClientCommand>>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<ClientDTO>(It.IsAny<Client>()))
            .Returns(default(ClientDTO)!);

        _handler = new RegisterClientHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _clientRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    private static RegisterClientCommand CreateValidIndividualCommand()
    {
        return new RegisterClientCommand(
            Name: "João da Silva",
            Email: "joao.silva@example.com",
            PhoneNumber: "11987654321",
            ClientType: ClientType.Individual,
            Cpf: "12345678901",
            Rg: "123456789",
            Cnh: "12345678901",
            Cnpj: null,
            State: "São Paulo",
            City: "São Paulo",
            District: "Centro",
            Street: "Rua das Flores",
            Number: "123"
        );
    }

    private static RegisterClientCommand CreateValidLegalEntityCommand()
    {
        return new RegisterClientCommand(
            Name: "Empresa ABC Ltda",
            Email: "contato@empresaabc.com",
            PhoneNumber: "1133334444",
            ClientType: ClientType.LegalEntity,
            Cpf: null,
            Rg: null,
            Cnh: null,
            Cnpj: "12345678000199",
            State: "Rio de Janeiro",
            City: "Rio de Janeiro",
            District: "Copacabana",
            Street: "Avenida Atlântica",
            Number: "456"
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

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        RegisterClientCommand command = CreateValidIndividualCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(RegisterClientCommand.Name), "O nome do cliente é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<ClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        RegisterClientCommand command = CreateValidIndividualCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<ClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _clientRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        RegisterClientCommand command = CreateValidIndividualCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<ClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Email_Already_Exists()
    {
        // arrange
        RegisterClientCommand command = CreateValidIndividualCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email))
            .ReturnsAsync(true);

        // act
        Result<ClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        
        var error = result.Errors.Single();
        Assert.IsTrue(
            error.Reasons.Any(r => 
                r.Message.Contains("Já existe um cliente cadastrado com este e-mail", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um cliente cadastrado com este e-mail.");

        _clientRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(command.Email), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Client>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_PhoneNumber_Already_Exists()
    {
        // arrange
        RegisterClientCommand command = CreateValidIndividualCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(command.PhoneNumber))
            .ReturnsAsync(true);

        // act
        Result<ClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        
        var error = result.Errors.Single();
        Assert.IsTrue(
            error.Reasons.Any(r => 
                r.Message.Contains("Já existe um cliente cadastrado com este telefone", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um cliente cadastrado com este telefone.");

        _clientRepositoryMock.Verify(r =>
            r.ExistsByPhoneNumberAsync(command.PhoneNumber), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Client>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Cpf_Already_Exists()
    {
        // arrange
        RegisterClientCommand command = CreateValidIndividualCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(command.PhoneNumber))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCpfAsync(command.Cpf!))
            .ReturnsAsync(true);

        // act
        Result<ClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        
        var error = result.Errors.Single();
        Assert.IsTrue(
            error.Reasons.Any(r => 
                r.Message.Contains("Já existe um cliente cadastrado com este CPF", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um cliente cadastrado com este CPF.");

        _clientRepositoryMock.Verify(r =>
            r.ExistsByCpfAsync(command.Cpf!), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Client>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Rg_Already_Exists()
    {
        // arrange
        RegisterClientCommand command = CreateValidIndividualCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(command.PhoneNumber))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCpfAsync(command.Cpf!))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByRgAsync(command.Rg!))
            .ReturnsAsync(true);

        // act
        Result<ClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        
        var error = result.Errors.Single();
        Assert.IsTrue(
            error.Reasons.Any(r => 
                r.Message.Contains("Já existe um cliente cadastrado com este RG", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um cliente cadastrado com este RG.");

        _clientRepositoryMock.Verify(r =>
            r.ExistsByRgAsync(command.Rg!), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Client>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Cnh_Already_Exists()
    {
        // arrange
        RegisterClientCommand command = CreateValidIndividualCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(command.PhoneNumber))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCpfAsync(command.Cpf!))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByRgAsync(command.Rg!))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCnhAsync(command.Cnh!))
            .ReturnsAsync(true);

        // act
        Result<ClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        
        var error = result.Errors.Single();
        Assert.IsTrue(
            error.Reasons.Any(r => 
                r.Message.Contains("Já existe um cliente cadastrado com esta CNH", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um cliente cadastrado com esta CNH.");

        _clientRepositoryMock.Verify(r =>
            r.ExistsByCnhAsync(command.Cnh!), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Client>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Cnpj_Already_Exists()
    {
        // arrange
        RegisterClientCommand command = CreateValidLegalEntityCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(command.PhoneNumber))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(r => r.ExistsByCnpjAsync(command.Cnpj!))
            .ReturnsAsync(true);

        // act
        Result<ClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        
        var error = result.Errors.Single();
        Assert.IsTrue(
            error.Reasons.Any(r => 
                r.Message.Contains("Já existe um cliente cadastrado com este CNPJ", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um cliente cadastrado com este CNPJ.");

        _clientRepositoryMock.Verify(r =>
            r.ExistsByCnpjAsync(command.Cnpj!), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Client>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Create_Individual_Client_And_Return_Success()
    {
        // arrange
        RegisterClientCommand command = CreateValidIndividualCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client? capturedClient = null;

        _clientRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Client>()))
            .Callback<Client>(c => capturedClient = c)
            .ReturnsAsync(Guid.NewGuid());

        var expectedDto = new ClientDTO(
            CreatedSuccessfully: true,
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
            .Setup(m => m.Map<ClientDTO>(It.IsAny<Client>()))
            .Returns(expectedDto);

        // act
        Result<ClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(capturedClient);
        Assert.AreNotEqual(Guid.Empty, capturedClient!.Id);
        Assert.AreEqual(companyUser.CompanyId ?? companyUser.Id, capturedClient.CompanyId);
        Assert.AreEqual(NameFormatter.FormatName(command.Name), capturedClient.Name);
        Assert.AreEqual(command.Email, capturedClient.Email);
        Assert.AreEqual(command.PhoneNumber, capturedClient.PhoneNumber);
        Assert.AreEqual(ClientType.Individual, capturedClient.ClientType);
        Assert.AreEqual(command.Cpf, capturedClient.Cpf);
        Assert.AreEqual(command.Rg, capturedClient.Rg);
        Assert.AreEqual(command.Cnh, capturedClient.Cnh);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(command.Email), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.ExistsByPhoneNumberAsync(command.PhoneNumber), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.ExistsByCpfAsync(command.Cpf!), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.ExistsByRgAsync(command.Rg!), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.ExistsByCnhAsync(command.Cnh!), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Client>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Create_LegalEntity_Client_And_Return_Success()
    {
        // arrange
        RegisterClientCommand command = CreateValidLegalEntityCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client? capturedClient = null;

        _clientRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Client>()))
            .Callback<Client>(c => capturedClient = c)
            .ReturnsAsync(Guid.NewGuid());

        var expectedDto = new ClientDTO(
            CreatedSuccessfully: true,
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
            .Setup(m => m.Map<ClientDTO>(It.IsAny<Client>()))
            .Returns(expectedDto);

        // act
        Result<ClientDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(capturedClient);
        Assert.AreNotEqual(Guid.Empty, capturedClient!.Id);
        Assert.AreEqual(companyUser.CompanyId ?? companyUser.Id, capturedClient.CompanyId);
        Assert.AreEqual(NameFormatter.FormatName(command.Name), capturedClient.Name);
        Assert.AreEqual(command.Email, capturedClient.Email);
        Assert.AreEqual(command.PhoneNumber, capturedClient.PhoneNumber);
        Assert.AreEqual(ClientType.LegalEntity, capturedClient.ClientType);
        Assert.AreEqual(command.Cnpj, capturedClient.Cnpj);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(command.Email), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.ExistsByPhoneNumberAsync(command.PhoneNumber), Times.Once);
        _clientRepositoryMock.Verify(r =>
            r.ExistsByCnpjAsync(command.Cnpj!), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Client>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        RegisterClientCommand command = CreateValidIndividualCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Client>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<ClientDTO> result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante o registro de cliente")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
