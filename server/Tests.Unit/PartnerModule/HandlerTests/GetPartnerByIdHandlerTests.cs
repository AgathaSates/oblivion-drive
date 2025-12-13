using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.PartnerModule.DTOs;
using OblivionDrive.Application.PartnerModule.Handlers;
using OblivionDrive.Application.PartnerModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.PartnerModule;

namespace OblivionDrive.Tests.Unit.PartnerModule.HandlerTests;

[TestClass]
[TestCategory("Partner - GetPartnerByIdHandler Unit Tests")]
public class GetPartnerByIdHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryPartner> _partnerRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetPartnerByIdHandler>> _loggerMock = default!;
    private Mock<IValidator<GetPartnerByIdQuery>> _validatorMock = default!;
    private GetPartnerByIdHandler _handler = default!;

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

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<DetailPartnerDTO>(It.IsAny<Partner>()))
            .Returns(default(DetailPartnerDTO)!);

        _loggerMock = new Mock<ILogger<GetPartnerByIdHandler>>();

        _validatorMock = new Mock<IValidator<GetPartnerByIdQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetPartnerByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetPartnerByIdHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _partnerRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetPartnerByIdQuery CreateValidQuery()
        => new GetPartnerByIdQuery(PartnerId: Guid.NewGuid());

    private static User CreateCompanyUser(Guid id, Guid? companyId = null)
        => new User
        {
            Id = id,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? id
        };

    private static Partner CreatePartner(Guid companyId, string name = "Parceiro XPTO")
        => new Partner(name, companyId);

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetPartnerByIdQuery query = CreateValidQuery();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetPartnerByIdQuery.PartnerId), "O identificador do parceiro é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<DetailPartnerDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<DetailPartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetPartnerByIdQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<DetailPartnerDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<DetailPartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetPartnerByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<DetailPartnerDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<DetailPartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Partner_Does_Not_Exist()
    {
        // arrange
        GetPartnerByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(query.PartnerId))
            .ReturnsAsync((Partner?)null);

        // act
        Result<DetailPartnerDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.PartnerId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailPartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Partner_Belongs_To_Other_Company()
    {
        // arrange
        GetPartnerByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Partner partnerFromOtherCompany = CreatePartner(otherCompanyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(query.PartnerId))
            .ReturnsAsync(partnerFromOtherCompany);

        // act
        Result<DetailPartnerDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.PartnerId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailPartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_PartnerDetail_When_Partner_Exists_And_Belongs_To_Current_Company()
    {
        // arrange
        GetPartnerByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Partner existingPartner = CreatePartner(companyId, "Parceiro Alpha");

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(query.PartnerId))
            .ReturnsAsync(existingPartner);

        var expectedDto = new DetailPartnerDTO(existingPartner.Id, existingPartner.Name);

        _mapperMock
            .Setup(m => m.Map<DetailPartnerDTO>(existingPartner))
            .Returns(expectedDto);

        // act
        Result<DetailPartnerDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.PartnerId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailPartnerDTO>(existingPartner), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_And_LogError_When_Exception_Occurs()
    {
        // arrange
        GetPartnerByIdQuery query = CreateValidQuery();

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
            .Setup(r => r.GetByIdAsync(query.PartnerId))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<DetailPartnerDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a obtenção de detalhes do parceiro")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}