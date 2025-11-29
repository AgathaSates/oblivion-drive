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
[TestCategory("Service - UpdateServiceHandler Unit Tests")]
public class UpdateServiceHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<IValidator<UpdateServiceCommand>> _validatorMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryServices> _serviceRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<UpdateServiceCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private UpdateServiceHandler _handler = default!;

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

        _validatorMock = new Mock<IValidator<UpdateServiceCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateServiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(Guid.NewGuid());

        _serviceRepositoryMock = new Mock<IRepositoryServices>();

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<UpdateServiceCommand>>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<UpdatedServiceDTO>(It.IsAny<Service>()))
            .Returns(default(UpdatedServiceDTO)!);

        _handler = new UpdateServiceHandler(
            _userManagerMock.Object,
            _validatorMock.Object,
            _tenantProviderMock.Object,
            _serviceRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    private static UpdateServiceCommand CreateValidCommand()
    {
        return new UpdateServiceCommand(
            ServiceId: Guid.NewGuid(),
            Name: "troca de óleo premium",
            Price: 250.00m,
            ChargeType: (ChargeType)1
        );
    }

    private static User CreateCompanyUser(Guid id, Guid? companyId = null)
    {
        return new User
        {
            Id = id,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? id
        };
    }

    private static Service CreateService(Guid companyId)
    {
        return new Service(
            name: "serviço original",
            price: 100m,
            chargeType: (ChargeType)1,
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        UpdateServiceCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(UpdateServiceCommand.Name), "O nome do serviço é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<UpdatedServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Service>(), It.IsAny<Service>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        UpdateServiceCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<UpdatedServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Service>(), It.IsAny<Service>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        UpdateServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<UpdatedServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Service>(), It.IsAny<Service>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Service_Does_Not_Exist()
    {
        // arrange
        UpdateServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        var companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ServiceId))
            .ReturnsAsync((Service?)null);

        // act
        Result<UpdatedServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ServiceId), Times.Once);

        _serviceRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Service>(), It.IsAny<Service>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Service_Belongs_To_Other_Company()
    {
        // arrange
        UpdateServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        var companyUser = CreateCompanyUser(currentUserId, companyId);
        var existingService = CreateService(otherCompanyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ServiceId))
            .ReturnsAsync(existingService);

        // act
        Result<UpdatedServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ServiceId), Times.Once);

        _serviceRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Service>(), It.IsAny<Service>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Update_Service_And_Return_Success()
    {
        // arrange
        UpdateServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        var companyUser = CreateCompanyUser(currentUserId, companyId);
        var existingService = CreateService(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ServiceId))
            .ReturnsAsync(existingService);

        Service? capturedExisting = null;
        Service? capturedUpdatedData = null;
        Service? returnedService = null;

        _serviceRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<Service>()))
            .Callback<Service, Service>((existing, updatedData) =>
            {
                capturedExisting = existing;
                capturedUpdatedData = updatedData;

                existing.Update(updatedData);
                returnedService = existing;
            })
            .ReturnsAsync(() => returnedService!);

        string expectedFormattedName = NameFormatter.FormatName(command.Name);

        var expectedDto = new UpdatedServiceDTO(
            UpdatedSuccessfully: true,
            Name: expectedFormattedName,
            Price: command.Price,
            ChargeType: command.ChargeType
        );

        _mapperMock
            .Setup(m => m.Map<UpdatedServiceDTO>(It.IsAny<Service>()))
            .Returns(expectedDto);

        // act
        Result<UpdatedServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(capturedUpdatedData);
        Assert.AreEqual(expectedFormattedName, capturedUpdatedData!.Name);
        Assert.AreEqual(command.Price, capturedUpdatedData.Price);
        Assert.AreEqual(command.ChargeType, capturedUpdatedData.ChargeType);
        Assert.AreEqual(existingService.CompanyId, capturedUpdatedData.CompanyId);

        Assert.IsNotNull(capturedExisting);
        Assert.AreEqual(expectedFormattedName, capturedExisting!.Name);
        Assert.AreEqual(command.Price, capturedExisting.Price);
        Assert.AreEqual(command.ChargeType, capturedExisting.ChargeType);
        Assert.AreEqual(companyId, capturedExisting.CompanyId);

        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.ServiceId), Times.Once);

        _serviceRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Service>(), It.IsAny<Service>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        UpdateServiceCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        var companyUser = CreateCompanyUser(currentUserId, companyId);
        var existingService = CreateService(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ServiceId))
            .ReturnsAsync(existingService);

        _serviceRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<Service>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<UpdatedServiceDTO> result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante a atualização de serviço")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
