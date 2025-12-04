using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.ServicesModule.Commands;
using OblivionDrive.Application.ServicesModule.DTOs;
using OblivionDrive.Application.ServicesModule.Handlers;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.ServicesModule.HandlerTests;

[TestClass]
[TestCategory("Service - RegisterServiceHandler Unit Tests")]
public class RegisterServiceHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryServices> _serviceRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IValidator<RegisterServiceCommand>> _validatorMock = default!;
    private Mock<ILogger<RegisterServiceCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private RegisterServiceHandler _handler = default!;

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

        _serviceRepositoryMock = new Mock<IRepositoryServices>();
        _serviceRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Service>()))
            .ReturnsAsync(Guid.NewGuid());

        _serviceRepositoryMock
            .Setup(r => r.ExistsByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<RegisterServiceCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterServiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<RegisterServiceCommand>>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<ServiceDTO>(It.IsAny<Service>()))
            .Returns(default(ServiceDTO)!);

        _handler = new RegisterServiceHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _serviceRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    private static RegisterServiceCommand CreateValidCommand()
    {
        return new RegisterServiceCommand(
            Name: "Troca de óleo premium",
            Price: 250.00m,
            ChargeType: (ChargeType)1
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
        RegisterServiceCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(RegisterServiceCommand.Name), "O nome do serviço é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<ServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Service>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        RegisterServiceCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<ServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Service>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        RegisterServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<ServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _serviceRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Service>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Create_Service_And_Return_Success()
    {
        // arrange
        RegisterServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Service? capturedService = null;

        _serviceRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Service>()))
            .Callback<Service>(s => capturedService = s)
            .ReturnsAsync(Guid.NewGuid());

        var expectedDto = new ServiceDTO(
            CreatedSuccessfully: true,
            Name: NameFormatter.FormatName(command.Name),
            Price: command.Price,
            ChargeType: command.ChargeType
        );

        _mapperMock
            .Setup(m => m.Map<ServiceDTO>(It.IsAny<Service>()))
            .Returns(expectedDto);

        // act
        Result<ServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(capturedService);
        Assert.AreNotEqual(Guid.Empty, capturedService!.Id);
        Assert.AreEqual(companyUser.CompanyId ?? companyUser.Id, capturedService.CompanyId);
        Assert.AreEqual(NameFormatter.FormatName(command.Name), capturedService.Name);
        Assert.AreEqual(command.Price, capturedService.Price);
        Assert.AreEqual(command.ChargeType, capturedService.ChargeType);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _serviceRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Service>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        RegisterServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Service>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<ServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante o registro de serviço")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Service_Name_Already_Exists()
    {
        // arrange
        RegisterServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.ExistsByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // act
        Result<ServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed, "Resultado deveria ser falha quando o nome do serviço já existe.");

        var error = result.Errors.Single();

        Assert.IsTrue(
            error.Metadata.TryGetValue("ErrorType", out object? errorType) &&
            string.Equals(errorType?.ToString(), "InvalidRequest", StringComparison.Ordinal),
            "ErrorType deveria ser 'InvalidRequest'."
        );

        Assert.IsTrue(
            error.Reasons.Any(r =>
                r.Message.Contains("Já existe um serviço cadastrado com este nome", StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um serviço cadastrado com este nome."
        );

        _serviceRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>()), Times.Once);

        _serviceRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Service>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }
}