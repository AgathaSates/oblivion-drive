using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.AuthenticationModule.Commands;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Application.AuthenticationModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;

namespace OblivionDrive.Tests.Unit.AuthenticationModule.HandlerTests;

[TestClass]
[TestCategory("Authentication - RegisterUserHandler Unit Tests")]
public class RegisterUserHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<IValidator<RegisterUserCommand>> _validatorMock = default!;
    private Mock<ITokenProvider> _tokenProviderMock = default!;
    private RegisterUserHandler _handler = default!;

    [TestInitialize]
    public void Setup()
    {
        var userStore = new Mock<IUserStore<User>>();
        var identityOptions = Options.Create(new IdentityOptions());
        var passwordHasher = new Mock<IPasswordHasher<User>>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>();
        var loggerUserManager = new Mock<ILogger<UserManager<User>>>();

        _userManagerMock = new Mock<UserManager<User>>(
            userStore.Object,
            identityOptions,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            keyNormalizer.Object,
            errors,
            services.Object,
            loggerUserManager.Object
        );

        _validatorMock = new Mock<IValidator<RegisterUserCommand>>();

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _tokenProviderMock = new Mock<ITokenProvider>();

        _handler = new RegisterUserHandler(
            _userManagerMock.Object,
            _validatorMock.Object,
            _tokenProviderMock.Object
        );
    }

    [TestMethod]
    public async Task Handle_Should_Return_AccessToken_When_Command_IsValid()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: "validUser",
            Email: "user@example.com",
            Password: "Senha123!"
        );

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), UserType.Company.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        var expectedToken = new AccessToken(
            key: "fake-jwt-token",
            expiration: DateTime.UtcNow.AddMinutes(15),
            authenticatedUser: new AuthenticatedUser(
                Id: Guid.NewGuid(),
                Name: "validUser",
                Email: "user@example.com",
                UserType: UserType.Company)
        );

        _tokenProviderMock
            .Setup(p => p.CreateAcessToken(It.IsAny<User>()))
            .Returns(expectedToken);

        // act
        Result<AccessToken> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedToken, result.Value);

        _userManagerMock.Verify(m =>
            m.CreateAsync(It.IsAny<User>(), command.Password), Times.Once);

        _userManagerMock.Verify(m =>
            m.AddToRoleAsync(It.IsAny<User>(), UserType.Company.ToString()), Times.Once);

        _tokenProviderMock.Verify(p =>
            p.CreateAcessToken(It.IsAny<User>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Invalid_Request_When_Validation_Fails()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: string.Empty,
            Email: "invalid-email",
            Password: "123"
        );

        var failures = new List<ValidationFailure>
        {
            new(nameof(RegisterUserCommand.UserName), "O nome de usuário é obrigatório."),
            new(nameof(RegisterUserCommand.Email), "O e-mail deve estar no formato [ nome@dominio.com ].")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // act
        Result<AccessToken> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);

        _tokenProviderMock.Verify(p =>
            p.CreateAcessToken(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_When_User_Creation_Fails()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: "validUser",
            Email: "user@example.com",
            Password: "Senha123!"
        );

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error creating user." }));

        // act
        Result<AccessToken> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);

        _tokenProviderMock.Verify(p =>
            p.CreateAcessToken(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_When_Add_To_Role_Fails()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: "validUser",
            Email: "user@example.com",
            Password: "Senha123!"
        );

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), UserType.Company.ToString()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error adding role." }));

        // act
        Result<AccessToken> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _tokenProviderMock.Verify(p =>
            p.CreateAcessToken(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_When_Token_Provider_Returns_Null()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: "validUser",
            Email: "user@example.com",
            Password: "Senha123!"
        );

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), UserType.Company.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenProviderMock
            .Setup(p => p.CreateAcessToken(It.IsAny<User>()))
            .Returns((IAccessToken?)null!);

        // act
        Result<AccessToken> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
    }
}