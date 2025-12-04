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
[TestCategory("VehicleGroup - GetVehicleGroupByIdHandler Unit Tests")]
public class GetVehicleGroupByIdHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryVehicleGroup> _vehicleGroupRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetVehicleGroupByIdHandler>> _loggerMock = default!;
    private Mock<IValidator<GetVehicleGroupByIdQuery>> _validatorMock = default!;
    private GetVehicleGroupByIdHandler _handler = default!;

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

        _loggerMock = new Mock<ILogger<GetVehicleGroupByIdHandler>>();

        _validatorMock = new Mock<IValidator<GetVehicleGroupByIdQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetVehicleGroupByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetVehicleGroupByIdHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _vehicleGroupRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetVehicleGroupByIdQuery CreateValidQuery()
    {
        return new GetVehicleGroupByIdQuery(
            VehicleGroupId: Guid.NewGuid()
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

    private static VehicleGroup CreateVehicleGroup(Guid companyId)
    {
        return new VehicleGroup(
            name: "Grupo Original",
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetVehicleGroupByIdQuery query = CreateValidQuery();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetVehicleGroupByIdQuery.VehicleGroupId), "O identificador do grupo de veículos é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<DetailVehicleGroupDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailVehicleGroupDTO>(It.IsAny<VehicleGroup>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetVehicleGroupByIdQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<DetailVehicleGroupDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailVehicleGroupDTO>(It.IsAny<VehicleGroup>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetVehicleGroupByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<DetailVehicleGroupDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailVehicleGroupDTO>(It.IsAny<VehicleGroup>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_VehicleGroup_Does_Not_Exist()
    {
        // arrange
        GetVehicleGroupByIdQuery query = CreateValidQuery();

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
            .Setup(r => r.GetByIdAsync(query.VehicleGroupId))
            .ReturnsAsync((VehicleGroup?)null);

        // act
        Result<DetailVehicleGroupDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.VehicleGroupId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailVehicleGroupDTO>(It.IsAny<VehicleGroup>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_VehicleGroup_Belongs_To_Other_Company()
    {
        // arrange
        GetVehicleGroupByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        VehicleGroup otherCompanyVehicleGroup = CreateVehicleGroup(otherCompanyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(query.VehicleGroupId))
            .ReturnsAsync(otherCompanyVehicleGroup);

        // act
        Result<DetailVehicleGroupDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.VehicleGroupId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailVehicleGroupDTO>(It.IsAny<VehicleGroup>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Success_When_VehicleGroup_Is_Found_And_Belongs_To_Company()
    {
        // arrange
        GetVehicleGroupByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        VehicleGroup existingVehicleGroup = CreateVehicleGroup(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _vehicleGroupRepositoryMock
            .Setup(r => r.GetByIdAsync(query.VehicleGroupId))
            .ReturnsAsync(existingVehicleGroup);

        var expectedDto = new DetailVehicleGroupDTO(
            Id: existingVehicleGroup.Id,
            Name: existingVehicleGroup.Name);

        _mapperMock
            .Setup(m => m.Map<DetailVehicleGroupDTO>(existingVehicleGroup))
            .Returns(expectedDto);

        // act
        Result<DetailVehicleGroupDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.VehicleGroupId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailVehicleGroupDTO>(existingVehicleGroup), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InternalExceptionError_When_Exception_Occurs()
    {
        // arrange
        GetVehicleGroupByIdQuery query = CreateValidQuery();

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
            .Setup(r => r.GetByIdAsync(query.VehicleGroupId))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<DetailVehicleGroupDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a obtenção de detalhes do grupo de veículos")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}