using System.Runtime.CompilerServices;
using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.PartnerModule.Commands;
using OblivionDrive.Application.PartnerModule.DTOs;
using OblivionDrive.Application.PartnerModule.Handlers;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.PartnerModule.HandlerTests;

[TestClass]
[TestCategory("Partner - RegisterPartnerHandler Unit Tests")]
public class RegisterPartnerHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryPartner> _partnerRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IValidator<RegisterPartnerCommand>> _validatorMock = default!;
    private Mock<ILogger<RegisterPartnerCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private RegisterPartnerHandler _handler = default!;

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

        _partnerRepositoryMock = new Mock<IRepositoryPartner>();
        _partnerRepositoryMock
            .Setup(r => r.ExistsByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _partnerRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Partner>()))
            .ReturnsAsync(Guid.NewGuid());

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<RegisterPartnerCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterPartnerCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<RegisterPartnerCommand>>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<PartnerDTO>(It.IsAny<Partner>()))
            .Returns(CreateUninitialized<PartnerDTO>());

        _handler = new RegisterPartnerHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _partnerRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    private static RegisterPartnerCommand CreateValidCommand()
        => new RegisterPartnerCommand(Name: "Parceiro Alpha");

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
        => new User
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? userId
        };

    private static T CreateUninitialized<T>() where T : class
        => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        RegisterPartnerCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(RegisterPartnerCommand.Name), "O nome do parceiro é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<PartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _partnerRepositoryMock.Verify(r => r.ExistsByNameAsync(It.IsAny<string>()), Times.Never);
        _partnerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        _mapperMock.Verify(m => m.Map<PartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        RegisterPartnerCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<PartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _partnerRepositoryMock.Verify(r => r.ExistsByNameAsync(It.IsAny<string>()), Times.Never);
        _partnerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        _mapperMock.Verify(m => m.Map<PartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        RegisterPartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<PartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _partnerRepositoryMock.Verify(r => r.ExistsByNameAsync(It.IsAny<string>()), Times.Never);
        _partnerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        _mapperMock.Verify(m => m.Map<PartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Partner_Name_Already_Exists()
    {
        // arrange
        RegisterPartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        string formattedName = NameFormatter.FormatName(command.Name);

        _partnerRepositoryMock
            .Setup(r => r.ExistsByNameAsync(formattedName))
            .ReturnsAsync(true);

        // act
        Result<PartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _partnerRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(formattedName), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        _mapperMock.Verify(m => m.Map<PartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Create_Partner_And_Return_Success()
    {
        // arrange
        RegisterPartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Partner? capturedPartner = null;

        _partnerRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Partner>()))
            .Callback<Partner>(partner => capturedPartner = partner)
            .ReturnsAsync(Guid.NewGuid());

        PartnerDTO expectedDto = CreateUninitialized<PartnerDTO>();

        _mapperMock
            .Setup(m => m.Map<PartnerDTO>(It.IsAny<Partner>()))
            .Returns(expectedDto);

        string formattedName = NameFormatter.FormatName(command.Name);

        // act
        Result<PartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        _partnerRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(formattedName), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Partner>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        Assert.IsNotNull(capturedPartner);
        Assert.AreNotEqual(Guid.Empty, capturedPartner!.Id);
        Assert.AreEqual(companyId, capturedPartner.CompanyId);
        Assert.AreEqual(formattedName, capturedPartner.Name);

        _mapperMock.Verify(m =>
            m.Map<PartnerDTO>(It.IsAny<Partner>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        RegisterPartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Partner>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<PartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante o registro de parceiro")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}