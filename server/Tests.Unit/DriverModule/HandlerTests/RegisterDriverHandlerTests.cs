
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.DriverModule.Commands;
using OblivionDrive.Application.DriverModule.DTOs;
using OblivionDrive.Application.DriverModule.Handlers;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.DriverModule.HandlerTests;

[TestClass]
[TestCategory("Driver - RegisterDriverHandler Unit Tests")]
public class RegisterDriverHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryDriver> _driverRepositoryMock = default!;
    private Mock<IRepositoryClient> _clientRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IValidator<RegisterDriverCommand>> _validatorMock = default!;
    private Mock<ILogger<RegisterDriverCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private RegisterDriverHandler _handler = default!;

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
            .Setup(r => r.AddAsync(It.IsAny<Driver>()))
            .ReturnsAsync(Guid.NewGuid());

        _driverRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _driverRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _driverRepositoryMock
            .Setup(r => r.ExistsByCpfAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _driverRepositoryMock
            .Setup(r => r.ExistsByCnhAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _clientRepositoryMock = new Mock<IRepositoryClient>();

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<RegisterDriverCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterDriverCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<RegisterDriverCommand>>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<DriverDTO>(It.IsAny<Driver>()))
            .Returns(CreateUninitialized<DriverDTO>());

        _handler = new RegisterDriverHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _driverRepositoryMock.Object,
            _clientRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    private static RegisterDriverCommand CreateValidCommand()
    {
        return new RegisterDriverCommand(
            Name: "joao da silva",
            Email: "joao.silva@email.com",
            PhoneNumber: "47999999999",
            Cpf: "12345678901",
            Cnh: "1234567890",
            CnhExpirationDate: DateOnly.FromDateTime(DateTime.Today).AddDays(1),
            ClientId: Guid.NewGuid(),
            IsClientAlsoDriver: false
        );
    }

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
        => new User
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? userId
        };

    private static Client CreateClient(Guid clientId, Guid companyId, ClientType clientType)
    {
        Client client = CreateUninitialized<Client>();

        SetMemberValue(client, "Id", clientId);
        SetMemberValue(client, "CompanyId", companyId);
        SetMemberValue(client, "ClientType", clientType);

        return client;
    }

    private static T CreateUninitialized<T>()
        => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static void SetMemberValue(object targetObject, string memberName, object? value)
    {
        Type targetType = targetObject.GetType();

        PropertyInfo? propertyInfo = targetType.GetProperty(
            memberName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (propertyInfo is not null && propertyInfo.CanWrite)
        {
            propertyInfo.SetValue(targetObject, value);
            return;
        }

        FieldInfo? fieldInfo =
            targetType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            targetType.GetField($"<{memberName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        if (fieldInfo is null)
            throw new InvalidOperationException($"Não foi possível setar '{memberName}' em '{targetType.FullName}'.");

        fieldInfo.SetValue(targetObject, value);
    }

    private static void AssertResultHasErrorMessage<T>(Result<T> result, string expectedSubstring)
    {
        bool hasExpectedMessage = result.Errors.Any(error =>
            (!string.IsNullOrWhiteSpace(error.Message) &&
             error.Message.Contains(expectedSubstring, StringComparison.CurrentCulture)) ||
            error.Reasons.Any(reason =>
                reason.Message.Contains(expectedSubstring, StringComparison.CurrentCulture)));

        Assert.IsTrue(hasExpectedMessage, $"Esperava encontrar '{expectedSubstring}' no resultado de erro.");
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(RegisterDriverCommand.Name), "O nome do condutor é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<DriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<DriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<DriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Client_Does_Not_Exist()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync((Client?)null);

        // act
        Result<DriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ClientId), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Client_Belongs_To_Other_Company()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Client clientFromOtherCompany = CreateClient(command.ClientId, otherCompanyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(clientFromOtherCompany);

        // act
        Result<DriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ClientId), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Client_Is_LegalEntity_And_IsClientAlsoDriver_Is_True()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { IsClientAlsoDriver = true };

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Client legalEntityClient = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(legalEntityClient);

        // act
        Result<DriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        AssertResultHasErrorMessage(result, "pessoa jurídica");

        _driverRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Email_Already_Exists()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Client client = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _driverRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email))
            .ReturnsAsync(true);

        // act
        Result<DriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        AssertResultHasErrorMessage(result, "Já existe um condutor cadastrado com este e-mail");

        _driverRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(command.Email), Times.Once);
        _driverRepositoryMock.Verify(r =>
            r.ExistsByPhoneNumberAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.ExistsByCpfAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.ExistsByCnhAsync(It.IsAny<string>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_PhoneNumber_Already_Exists()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Client client = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _driverRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email))
            .ReturnsAsync(false);

        _driverRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(command.PhoneNumber))
            .ReturnsAsync(true);

        // act
        Result<DriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        AssertResultHasErrorMessage(result, "Já existe um condutor cadastrado com este telefone");

        _driverRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(command.Email), Times.Once);
        _driverRepositoryMock.Verify(r =>
            r.ExistsByPhoneNumberAsync(command.PhoneNumber), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.ExistsByCpfAsync(It.IsAny<string>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.ExistsByCnhAsync(It.IsAny<string>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Cpf_Already_Exists()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Client client = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _driverRepositoryMock
            .Setup(r => r.ExistsByCpfAsync(command.Cpf))
            .ReturnsAsync(true);

        // act
        Result<DriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        AssertResultHasErrorMessage(result, "Já existe um condutor cadastrado com este CPF");

        _driverRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(command.Email), Times.Once);
        _driverRepositoryMock.Verify(r =>
            r.ExistsByPhoneNumberAsync(command.PhoneNumber), Times.Once);
        _driverRepositoryMock.Verify(r =>
            r.ExistsByCpfAsync(command.Cpf), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.ExistsByCnhAsync(It.IsAny<string>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Cnh_Already_Exists()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Client client = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _driverRepositoryMock
            .Setup(r => r.ExistsByCnhAsync(command.Cnh))
            .ReturnsAsync(true);

        // act
        Result<DriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        AssertResultHasErrorMessage(result, "Já existe um condutor cadastrado com esta CNH");

        _driverRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(command.Email), Times.Once);
        _driverRepositoryMock.Verify(r =>
            r.ExistsByPhoneNumberAsync(command.PhoneNumber), Times.Once);
        _driverRepositoryMock.Verify(r =>
            r.ExistsByCpfAsync(command.Cpf), Times.Once);
        _driverRepositoryMock.Verify(r =>
            r.ExistsByCnhAsync(command.Cnh), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Create_Driver_And_Return_Success()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client client = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        Driver? capturedDriver = null;

        _driverRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Driver>()))
            .Callback<Driver>(driver => capturedDriver = driver)
            .ReturnsAsync(Guid.NewGuid());

        DriverDTO expectedDto = CreateUninitialized<DriverDTO>();

        _mapperMock
            .Setup(m => m.Map<DriverDTO>(It.IsAny<Driver>()))
            .Returns(expectedDto);

        string expectedFormattedName = NameFormatter.FormatName(command.Name);

        // act
        Result<DriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        _mapperMock.Verify(m => m.Map<DriverDTO>(It.IsAny<Driver>()), Times.Once);

        Assert.IsNotNull(capturedDriver);
        Assert.AreNotEqual(Guid.Empty, capturedDriver!.Id);
        Assert.AreEqual(companyId, capturedDriver.CompanyId);
        Assert.AreEqual(command.ClientId, capturedDriver.ClientId);

        Assert.AreEqual(expectedFormattedName, capturedDriver.Name);
        Assert.AreEqual(command.Email, capturedDriver.Email);
        Assert.AreEqual(command.PhoneNumber, capturedDriver.PhoneNumber);
        Assert.AreEqual(command.Cpf, capturedDriver.Cpf);
        Assert.AreEqual(command.Cnh, capturedDriver.Cnh);
        Assert.AreEqual(command.CnhExpirationDate, capturedDriver.CnhExpirationDate);
        Assert.AreEqual(command.IsClientAlsoDriver, capturedDriver.IsClientAlsoDriver);

        _driverRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Driver>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Client client = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _driverRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Driver>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<DriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante o registro de condutor")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}