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
[TestCategory("Service - GetAllServicesHandler Unit Tests")]
public class GetAllServicesHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryServices> _serviceRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetAllServicesHandler>> _loggerMock = default!;
    private Mock<IValidator<GetAllServicesQuery>> _validatorMock = default!;
    private GetAllServicesHandler _handler = default!;

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

        _loggerMock = new Mock<ILogger<GetAllServicesHandler>>();

        _validatorMock = new Mock<IValidator<GetAllServicesQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetAllServicesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetAllServicesHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _serviceRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetAllServicesQuery CreateQueryWithoutQuantity()
        => new GetAllServicesQuery(Quantity: null);

    private static GetAllServicesQuery CreateQueryWithQuantity(int quantity)
        => new GetAllServicesQuery(Quantity: quantity);

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

    private static Service CreateService(Guid companyId, string name, decimal price, ChargeType chargeType)
    {
        return new Service(
            name: name,
            price: price,
            chargeType: chargeType,
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetAllServicesQuery query = CreateQueryWithoutQuantity();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetAllServicesQuery.Quantity), "A quantidade deve ser maior que zero.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<ServicesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailServiceDTO>>(It.IsAny<IReadOnlyCollection<Service>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetAllServicesQuery query = CreateQueryWithoutQuantity();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<ServicesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailServiceDTO>>(It.IsAny<IReadOnlyCollection<Service>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetAllServicesQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<ServicesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _serviceRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _serviceRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailServiceDTO>>(It.IsAny<IReadOnlyCollection<Service>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_All_Services_When_Quantity_Is_Null()
    {
        // arrange
        GetAllServicesQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var services = new List<Service>
        {
            CreateService(companyId, "Lavagem simples", 50m, (ChargeType)1),
            CreateService(companyId, "Lavagem completa", 80m, (ChargeType)1),
            CreateService(companyId, "Polimento", 150m, (ChargeType)2),
        };

        _serviceRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(services);

        var detailList = new List<DetailServiceDTO>
        {
            new(services[0].Id, services[0].Name, services[0].Price, services[0].ChargeType),
            new(services[1].Id, services[1].Name, services[1].Price, services[1].ChargeType),
            new(services[2].Id, services[2].Name, services[2].Price, services[2].ChargeType)
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailServiceDTO>>(services))
            .Returns(detailList);

        // act
        Result<ServicesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.IsNotNull(result.Value.Services);
        Assert.AreEqual(detailList.Count, result.Value.Services.Count);
        CollectionAssert.AreEquivalent(detailList, result.Value.Services.ToList());

        _serviceRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);
        _serviceRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailServiceDTO>>(services), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Call_GetAll_With_Quantity_When_Quantity_Is_Provided()
    {
        // arrange
        const int quantity = 2;
        GetAllServicesQuery query = CreateQueryWithQuantity(quantity);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var services = new List<Service>
        {
            CreateService(companyId, "Serviço 1", 10m, (ChargeType)1),
            CreateService(companyId, "Serviço 2", 20m, (ChargeType)1),
        };

        _serviceRepositoryMock
            .Setup(r => r.GetAllAsync(quantity))
            .ReturnsAsync(services);

        var detailList = new List<DetailServiceDTO>
        {
            new(services[0].Id, services[0].Name, services[0].Price, services[0].ChargeType),
            new(services[1].Id, services[1].Name, services[1].Price, services[1].ChargeType)
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailServiceDTO>>(services))
            .Returns(detailList);

        // act
        Result<ServicesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(detailList.Count, result.Value.Services.Count);

        _serviceRepositoryMock.Verify(r =>
            r.GetAllAsync(quantity), Times.Once);
        _serviceRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailServiceDTO>>(services), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_And_LogError_When_Exception_Occurs()
    {
        // arrange
        GetAllServicesQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _serviceRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<ServicesResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a listagem de serviços da empresa")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}