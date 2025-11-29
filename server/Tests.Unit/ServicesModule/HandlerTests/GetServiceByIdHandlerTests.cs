using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.ServicesModule.DTOs;
using OblivionDrive.Application.ServicesModule.Handlers;
using OblivionDrive.Application.ServicesModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Tests.Unit.ServicesModule.HandlerTests;

[TestClass]
[TestCategory("Service - GetServiceByIdHandler Unit Tests")]
public class GetServiceByIdHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryServices> _serviceRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetServiceByIdHandler>> _loggerMock = default!;
    private Mock<IValidator<GetServiceByIdQuery>> _validatorMock = default!;
    private GetServiceByIdHandler _handler = default!;

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

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<DetailServiceDTO>(It.IsAny<Service>()))
            .Returns(default(DetailServiceDTO)!);

        _loggerMock = new Mock<ILogger<GetServiceByIdHandler>>();

        _validatorMock = new Mock<IValidator<GetServiceByIdQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetServiceByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetServiceByIdHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _serviceRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetServiceByIdQuery CreateValidQuery()
    {
        return new GetServiceByIdQuery(ServiceId: Guid.NewGuid());
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
            name: "Serviço de lavagem",
            price: 150m,
            chargeType: (ChargeType)1,
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetServiceByIdQuery query = CreateValidQuery();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetServiceByIdQuery.ServiceId), "O identificador do serviço é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<DetailServiceDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailServiceDTO>(It.IsAny<Service>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetServiceByIdQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<DetailServiceDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailServiceDTO>(It.IsAny<Service>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetServiceByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<DetailServiceDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailServiceDTO>(It.IsAny<Service>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Service_Does_Not_Exist()
    {
        // arrange
        GetServiceByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(query.ServiceId))
            .ReturnsAsync((Service?)null);

        // act
        Result<DetailServiceDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.ServiceId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailServiceDTO>(It.IsAny<Service>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Service_Belongs_To_Other_Company()
    {
        // arrange
        GetServiceByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Service serviceFromOtherCompany = CreateService(otherCompanyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(query.ServiceId))
            .ReturnsAsync(serviceFromOtherCompany);

        // act
        Result<DetailServiceDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.ServiceId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailServiceDTO>(It.IsAny<Service>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_ServiceDetail_When_Service_Exists_And_Belongs_To_Current_Company()
    {
        // arrange
        GetServiceByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Service existingService = CreateService(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(query.ServiceId))
            .ReturnsAsync(existingService);

        var expectedDetail = new DetailServiceDTO(
            Id: existingService.Id,
            Name: existingService.Name,
            Price: existingService.Price,
            ChargeType: existingService.ChargeType
        );

        _mapperMock
            .Setup(m => m.Map<DetailServiceDTO>(existingService))
            .Returns(expectedDetail);

        // act
        Result<DetailServiceDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDetail, result.Value);

        _serviceRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.ServiceId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailServiceDTO>(existingService), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_And_LogError_When_Exception_Occurs()
    {
        // arrange
        GetServiceByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Service existingService = CreateService(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(query.ServiceId))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<DetailServiceDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a obtenção de detalhes do serviço")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}