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
[TestCategory("Driver - UpdateDriverHandler Unit Tests")]
public class UpdateDriverHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<IValidator<UpdateDriverCommand>> _validatorMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryDriver> _driverRepositoryMock = default!;
    private Mock<IRepositoryClient> _clientRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<UpdateDriverCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private UpdateDriverHandler _handler = default!;

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

        _validatorMock = new Mock<IValidator<UpdateDriverCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateDriverCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(Guid.NewGuid());

        _driverRepositoryMock = new Mock<IRepositoryDriver>();

        _clientRepositoryMock = new Mock<IRepositoryClient>();

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<UpdateDriverCommand>>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<UpdatedDriverDTO>(It.IsAny<Driver>()))
            .Returns(CreateUninitialized<UpdatedDriverDTO>());

        _handler = new UpdateDriverHandler(
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

    private static UpdateDriverCommand CreateValidCommand()
    {
        return new UpdateDriverCommand(
            DriverId: Guid.NewGuid(),
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

    private static Driver CreateDriver(Guid companyId, Guid driverId)
    {
        Driver driver = new Driver(
            companyId: companyId,
            clientId: Guid.NewGuid(),
            name: "Condutor Original",
            phoneNumber: "47999999999",
            cpf: "12345678901",
            cnh: "1234567890",
            cnhExpirationDate: DateOnly.FromDateTime(DateTime.Today).AddDays(10),
            email: "original@email.com",
            isClientAlsoDriver: false);

        SetMemberValue(driver, "Id", driverId);
        SetMemberValue(driver, "CompanyId", companyId);

        return driver;
    }


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
        UpdateDriverCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(UpdateDriverCommand.Name), "O nome do condutor é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        UpdateDriverCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        UpdateDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Driver_Does_Not_Exist()
    {
        // arrange
        UpdateDriverCommand command = CreateValidCommand();

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
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.DriverId), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Driver_Belongs_To_Other_Company()
    {
        // arrange
        UpdateDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Driver driverFromOtherCompany = CreateDriver(otherCompanyId, command.DriverId);

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
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _driverRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.DriverId), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Client_Does_Not_Exist()
    {
        // arrange
        UpdateDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        Driver existingDriver = CreateDriver(companyId, command.DriverId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync(existingDriver);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync((Client?)null);

        // act
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ClientId), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Client_Belongs_To_Other_Company()
    {
        // arrange
        UpdateDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Driver existingDriver = CreateDriver(companyId, command.DriverId);

        Client clientFromOtherCompany = CreateClient(command.ClientId, otherCompanyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync(existingDriver);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(clientFromOtherCompany);

        // act
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _clientRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ClientId), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Email_Already_Exists_For_Another_Driver()
    {
        // arrange
        UpdateDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Driver existingDriver = CreateDriver(companyId, command.DriverId);
        Client client = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync(existingDriver);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _driverRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email, command.DriverId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        AssertResultHasErrorMessage(result, "Já existe um condutor cadastrado com este e-mail");

        _driverRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(command.Email, command.DriverId), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.ExistsByPhoneNumberAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.ExistsByCpfAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _driverRepositoryMock.Verify(r =>
            r.ExistsByCnhAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_PhoneNumber_Already_Exists_For_Another_Driver()
    {
        // arrange
        UpdateDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Driver existingDriver = CreateDriver(companyId, command.DriverId);
        Client client = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync(existingDriver);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _driverRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(command.Email, command.DriverId))
            .ReturnsAsync(false);

        _driverRepositoryMock
            .Setup(r => r.ExistsByPhoneNumberAsync(command.PhoneNumber, command.DriverId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        AssertResultHasErrorMessage(result, "Já existe um condutor cadastrado com este telefone");

        _driverRepositoryMock.Verify(r =>
            r.ExistsByEmailAsync(command.Email, command.DriverId), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.ExistsByPhoneNumberAsync(command.PhoneNumber, command.DriverId), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.ExistsByCpfAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.ExistsByCnhAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Cpf_Already_Exists_For_Another_Driver()
    {
        // arrange
        UpdateDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Driver existingDriver = CreateDriver(companyId, command.DriverId);
        Client client = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync(existingDriver);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _driverRepositoryMock
            .Setup(r => r.ExistsByCpfAsync(command.Cpf, command.DriverId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        AssertResultHasErrorMessage(result, "Já existe um condutor cadastrado com este CPF");

        _driverRepositoryMock.Verify(r =>
            r.ExistsByCpfAsync(command.Cpf, command.DriverId), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.ExistsByCnhAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);

        _driverRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Cnh_Already_Exists_For_Another_Driver()
    {
        // arrange
        UpdateDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Driver existingDriver = CreateDriver(companyId, command.DriverId);
        Client client = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync(existingDriver);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _driverRepositoryMock
            .Setup(r => r.ExistsByCnhAsync(command.Cnh, command.DriverId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        AssertResultHasErrorMessage(result, "Já existe um condutor cadastrado com esta CNH");

        _driverRepositoryMock.Verify(r =>
            r.ExistsByCnhAsync(command.Cnh, command.DriverId), Times.Once);

        _driverRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Update_Driver_And_Return_Success()
    {
        // arrange
        UpdateDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Driver existingDriver = CreateDriver(companyId, command.DriverId);
        Client client = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync(existingDriver);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        Driver? capturedExisting = null;
        Driver? capturedUpdatedData = null;

        _driverRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()))
            .Callback<Driver, Driver>((existing, updatedData) =>
            {
                capturedExisting = existing;
                capturedUpdatedData = updatedData;

                existing.Update(updatedData);
            })
            .ReturnsAsync(() => capturedExisting!);

        string expectedFormattedName = NameFormatter.FormatName(command.Name);

        UpdatedDriverDTO expectedDto = CreateUninitialized<UpdatedDriverDTO>();

        _mapperMock
            .Setup(m => m.Map<UpdatedDriverDTO>(It.IsAny<Driver>()))
            .Returns(expectedDto);

        // act
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        Assert.IsNotNull(capturedUpdatedData);
        Assert.AreEqual(command.DriverId, capturedExisting!.Id);
        Assert.AreEqual(companyId, capturedExisting.CompanyId);

        Assert.AreEqual(expectedFormattedName, capturedUpdatedData!.Name);
        Assert.AreEqual(command.Email, capturedUpdatedData.Email);
        Assert.AreEqual(command.PhoneNumber, capturedUpdatedData.PhoneNumber);
        Assert.AreEqual(command.Cpf, capturedUpdatedData.Cpf);
        Assert.AreEqual(command.Cnh, capturedUpdatedData.Cnh);
        Assert.AreEqual(command.CnhExpirationDate, capturedUpdatedData.CnhExpirationDate);
        Assert.AreEqual(command.ClientId, capturedUpdatedData.ClientId);
        Assert.AreEqual(command.IsClientAlsoDriver, capturedUpdatedData.IsClientAlsoDriver);

        Assert.AreEqual(expectedFormattedName, capturedExisting.Name);
        Assert.AreEqual(command.Email, capturedExisting.Email);
        Assert.AreEqual(command.PhoneNumber, capturedExisting.PhoneNumber);
        Assert.AreEqual(command.Cpf, capturedExisting.Cpf);
        Assert.AreEqual(command.Cnh, capturedExisting.Cnh);
        Assert.AreEqual(command.CnhExpirationDate, capturedExisting.CnhExpirationDate);
        Assert.AreEqual(command.ClientId, capturedExisting.ClientId);
        Assert.AreEqual(command.IsClientAlsoDriver, capturedExisting.IsClientAlsoDriver);

        _driverRepositoryMock.Verify(r => r.GetByIdAsync(command.DriverId), Times.Once);
        _clientRepositoryMock.Verify(r => r.GetByIdAsync(command.ClientId), Times.Once);
        _driverRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        UpdateDriverCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Driver existingDriver = CreateDriver(companyId, command.DriverId);
        Client client = CreateClient(command.ClientId, companyId, ClientType.LegalEntity);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _driverRepositoryMock
            .Setup(r => r.GetByIdAsync(command.DriverId))
            .ReturnsAsync(existingDriver);

        _clientRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ClientId))
            .ReturnsAsync(client);

        _driverRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Driver>(), It.IsAny<Driver>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<UpdatedDriverDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a atualização do condutor")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}