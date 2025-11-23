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
[TestCategory("Authentication - LoginUserHandler Unit Tests")]
public class LoginUserHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<SignInManager<User>> _signInManagerMock = default!;
    private Mock<IValidator<LoginUserCommand>> _validatorMock = default!;
    private Mock<ITokenProvider> _tokenProviderMock = default!;
    private LoginUserHandler _handler = default!;

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

        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        var loggerSignInManager = new Mock<ILogger<SignInManager<User>>>();
        var schemeProvider = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        var confirmation = new Mock<IUserConfirmation<User>>();

        _signInManagerMock = new Mock<SignInManager<User>>(
            _userManagerMock.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            identityOptions,
            loggerSignInManager.Object,
            schemeProvider.Object,
            confirmation.Object
        );

        _validatorMock = new Mock<IValidator<LoginUserCommand>>();

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _tokenProviderMock = new Mock<ITokenProvider>();

        _handler = new LoginUserHandler(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _validatorMock.Object,
            _tokenProviderMock.Object
        );
    }

    private static LoginUserCommand CreateValidCommand()
    {
        return new LoginUserCommand(
            UserName: "validUser",
            Password: "Senha123!"
        );
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        var command = CreateValidCommand();

        var failures = new List<ValidationFailure>
        {
            new(nameof(LoginUserCommand.UserName), "O nome de usuário é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // act
        Result<AccessToken> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByNameAsync(It.IsAny<string>()), Times.Never);

        _signInManagerMock.Verify(m =>
            m.PasswordSignInAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()),
            Times.Never);

        _tokenProviderMock.Verify(p =>
            p.CreateAcessToken(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_UserNotFoundError_When_User_Does_Not_Exist()
    {
        // arrange
        var command = CreateValidCommand();

        _userManagerMock
            .Setup(m => m.FindByNameAsync(command.UserName))
            .ReturnsAsync((User?)null);

        // act
        Result<AccessToken> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _signInManagerMock.Verify(m =>
            m.PasswordSignInAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()),
            Times.Never);

        _tokenProviderMock.Verify(p =>
            p.CreateAcessToken(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_IncorrectCredentialsError_When_Password_SignIn_Fails()
    {
        // arrange
        var command = CreateValidCommand();

        var existingUser = new User
        {
            UserName = command.UserName,
            Email = "user@example.com",
            UserType = UserType.Company
        };

        _userManagerMock
            .Setup(m => m.FindByNameAsync(command.UserName))
            .ReturnsAsync(existingUser);

        _signInManagerMock
            .Setup(m => m.PasswordSignInAsync(
                command.UserName,
                command.Password,
                false,
                true))
            .ReturnsAsync(SignInResult.Failed);

        // act
        Result<AccessToken> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _tokenProviderMock.Verify(p =>
            p.CreateAcessToken(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InternalException_When_TokenProvider_Returns_Null()
    {
        // arrange
        var command = CreateValidCommand();

        var existingUser = new User
        {
            UserName = command.UserName,
            Email = "user@example.com",
            UserType = UserType.Company
        };

        _userManagerMock
            .Setup(m => m.FindByNameAsync(command.UserName))
            .ReturnsAsync(existingUser);

        _signInManagerMock
            .Setup(m => m.PasswordSignInAsync(
                command.UserName,
                command.Password,
                false,
                true))
            .ReturnsAsync(SignInResult.Success);

        _tokenProviderMock
            .Setup(p => p.CreateAcessToken(existingUser))
            .Returns((IAccessToken?)null!);

        // act
        Result<AccessToken> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
    }

    [TestMethod]
    public async Task Handle_Should_Return_AccessToken_When_Credentials_Are_Valid()
    {
        // arrange
        var command = CreateValidCommand();

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = command.UserName,
            Email = "user@example.com",
            UserType = UserType.Company
        };

        _userManagerMock
            .Setup(m => m.FindByNameAsync(command.UserName))
            .ReturnsAsync(existingUser);

        _signInManagerMock
            .Setup(m => m.PasswordSignInAsync(
                command.UserName,
                command.Password,
                false,
                true))
            .ReturnsAsync(SignInResult.Success);

        var expectedToken = new AccessToken(
            key: "fake-login-jwt-token",
            expiration: DateTime.UtcNow.AddMinutes(15),
            authenticatedUser: new AuthenticatedUser(
                Id: existingUser.Id,
                Name: existingUser.UserName!,
                Email: existingUser.Email ?? string.Empty,
                UserType: existingUser.UserType)
        );

        _tokenProviderMock
            .Setup(p => p.CreateAcessToken(existingUser))
            .Returns(expectedToken);

        // act
        Result<AccessToken> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedToken, result.Value);

        _userManagerMock.Verify(m =>
            m.FindByNameAsync(command.UserName), Times.Once);

        _signInManagerMock.Verify(m =>
            m.PasswordSignInAsync(
                command.UserName,
                command.Password,
                false,
                true),
            Times.Once);

        _tokenProviderMock.Verify(p =>
            p.CreateAcessToken(existingUser), Times.Once);
    }
}