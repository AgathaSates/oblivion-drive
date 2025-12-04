
using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.VehicleGroupModule.DTOs;
using OblivionDrive.Application.VehicleGroupModule.Handlers;
using OblivionDrive.Application.VehicleGroupModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.VehicleGroupModule;

namespace OblivionDrive.Tests.Unit.VehicleGroupModule.HandlerTests;

[TestClass]
[TestCategory("VehicleGroup - GetAllVehicleGroupHandler Unit Tests")]
public class GetAllVehicleGroupHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryVehicleGroup> _vehicleGroupRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetAllVehicleGroupHandler>> _loggerMock = default!;
    private Mock<IValidator<GetAllVehicleGroupQuery>> _validatorMock = default!;
    private GetAllVehicleGroupHandler _handler = default!;

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

        _vehicleGroupRepositoryMock = new Mock<IRepositoryVehicleGroup>();

        _mapperMock = new Mock<IMapper>();

        _loggerMock = new Mock<ILogger<GetAllVehicleGroupHandler>>();

        _validatorMock = new Mock<IValidator<GetAllVehicleGroupQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetAllVehicleGroupQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetAllVehicleGroupHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _vehicleGroupRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetAllVehicleGroupQuery CreateValidQuery(int? quantity = 10)
    {
        return new GetAllVehicleGroupQuery(
            Quantity: quantity
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

    private static VehicleGroup CreateVehicleGroup(Guid companyId, string name)
    {
        return new VehicleGroup(
            name: name,
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetAllVehicleGroupQuery query = CreateValidQuery();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetAllVehicleGroupQuery.Quantity), "A quantidade deve ser maior que zero.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<VehicleGroupResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailVehicleGroupDTO>>(It.IsAny<IReadOnlyCollection<VehicleGroup>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetAllVehicleGroupQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<VehicleGroupResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailVehicleGroupDTO>>(It.IsAny<IReadOnlyCollection<VehicleGroup>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetAllVehicleGroupQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<VehicleGroupResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailVehicleGroupDTO>>(It.IsAny<IReadOnlyCollection<VehicleGroup>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Success_When_Quantity_Is_Null()
    {
        // arrange
        GetAllVehicleGroupQuery query = CreateValidQuery(quantity: null);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var vehicleGroups = new List<VehicleGroup>
        {
            CreateVehicleGroup(companyId, "Grupo A"),
            CreateVehicleGroup(companyId, "Grupo B")
        };

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(vehicleGroups);

        var expectedDtos = new List<DetailVehicleGroupDTO>
        {
            new(vehicleGroups[0].Id, vehicleGroups[0].Name),
            new(vehicleGroups[1].Id, vehicleGroups[1].Name)
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailVehicleGroupDTO>>(vehicleGroups))
            .Returns(expectedDtos);

        // act
        Result<VehicleGroupResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(2, result.Value.VehicleGroups.Count);
        CollectionAssert.AreEquivalent(
            expectedDtos,
            result.Value.VehicleGroups.ToList());

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailVehicleGroupDTO>>(vehicleGroups), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Success_When_Quantity_Is_Specified()
    {
        // arrange
        const int quantity = 5;
        GetAllVehicleGroupQuery query = CreateValidQuery(quantity);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var vehicleGroups = new List<VehicleGroup>
        {
            CreateVehicleGroup(companyId, "Grupo 1"),
            CreateVehicleGroup(companyId, "Grupo 2"),
        };

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetAllAsync(quantity))
            .ReturnsAsync(vehicleGroups);

        var expectedDtos = new List<DetailVehicleGroupDTO>
        {
            new(vehicleGroups[0].Id, vehicleGroups[0].Name),
            new(vehicleGroups[1].Id, vehicleGroups[1].Name)
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailVehicleGroupDTO>>(vehicleGroups))
            .Returns(expectedDtos);

        // act
        Result<VehicleGroupResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(2, result.Value.VehicleGroups.Count);
        CollectionAssert.AreEquivalent(
            expectedDtos,
            result.Value.VehicleGroups.ToList());

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetAllAsync(quantity), Times.Once);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailVehicleGroupDTO>>(vehicleGroups), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InternalExceptionError_When_Exception_Occurs()
    {
        // arrange
        GetAllVehicleGroupQuery query = CreateValidQuery(quantity: null);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<VehicleGroupResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a listagem de grupos de veículos da empresa")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}