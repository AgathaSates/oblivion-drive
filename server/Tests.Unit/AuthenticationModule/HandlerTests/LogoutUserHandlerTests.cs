using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.AuthenticationModule.Commands;
using OblivionDrive.Application.AuthenticationModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;

namespace OblivionDrive.Tests.Unit.AuthenticationModule.HandlerTests;

[TestClass]
[TestCategory("Authentication - LogoutUserHandler Unit Tests")]
public class LogoutUserHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<SignInManager<User>> _signInManagerMock = default!;
    private LogoutUserHandler _handler = default!;

    [TestInitialize]
    public void Setup()
    {
        // UserManager<User> mock
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

        // SignInManager<User> mock
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

        _handler = new LogoutUserHandler(_signInManagerMock.Object);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Success_Result_When_SignOut_Completes()
    {
        // arrange
        _signInManagerMock
            .Setup(m => m.SignOutAsync())
            .Returns(Task.CompletedTask);

        var command = new LogoutUserCommand();

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.IsFailed);
    }

    [TestMethod]
    public async Task Handle_Should_Call_SignOutAsync_When_Handling_LogoutCommand()
    {
        // arrange
        _signInManagerMock
            .Setup(m => m.SignOutAsync())
            .Returns(Task.CompletedTask);

        var command = new LogoutUserCommand();

        // act
        await _handler.Handle(command, CancellationToken.None);

        // assert
        _signInManagerMock.Verify(m =>
            m.SignOutAsync(), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_ThrowException_When_SignOutAsync_Throws()
    {
        // arrange
        _signInManagerMock
            .Setup(m => m.SignOutAsync())
            .ThrowsAsync(new InvalidOperationException("Sign-out failed."));

        var command = new LogoutUserCommand();

        // act + assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await _handler.Handle(command, CancellationToken.None);
        });
    }
}