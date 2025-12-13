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
[TestCategory("Partner - GetAllPartnersHandler Unit Tests")]
public class GetAllPartnersHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryPartner> _partnerRepositoryMock = default!;
    private Mock<IValidator<GetAllPartnersQuery>> _validatorMock = default!;
    private Mock<ILogger<GetAllPartnersHandler>> _loggerMock = default!;
    private Mock<AutoMapper.IMapper> _mapperMock = default!;

    private GetAllPartnersHandler _handler = default!;

    [TestInitialize]
    public void Setup()
    {
        var userStoreMock = new Mock<IUserStore<User>>();
        var identityOptions = Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<User>>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorDescriber = new IdentityErrorDescriber();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var loggerUserManagerMock = new Mock<ILogger<UserManager<User>>>();

        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object,
            identityOptions,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            keyNormalizerMock.Object,
            errorDescriber,
            serviceProviderMock.Object,
            loggerUserManagerMock.Object
        );

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(Guid.NewGuid());

        _partnerRepositoryMock = new Mock<IRepositoryPartner>();

        _mapperMock = new Mock<AutoMapper.IMapper>();

        _loggerMock = new Mock<ILogger<GetAllPartnersHandler>>();

        _validatorMock = new Mock<IValidator<GetAllPartnersQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetAllPartnersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetAllPartnersHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _partnerRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetAllPartnersQuery CreateQueryWithoutQuantity()
        => new GetAllPartnersQuery(Quantity: null);

    private static GetAllPartnersQuery CreateQueryWithQuantity(int quantity)
        => new GetAllPartnersQuery(Quantity: quantity);

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
        => new User
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? userId
        };

    private static Partner CreatePartner(Guid companyId, string name)
        => new Partner(name: name, companyId: companyId);


    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetAllPartnersQuery query = CreateQueryWithoutQuantity();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetAllPartnersQuery.Quantity), "A quantidade deve ser maior que zero.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<PartnersResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _partnerRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _partnerRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailPartnerDTO>>(It.IsAny<IReadOnlyCollection<Partner>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetAllPartnersQuery query = CreateQueryWithoutQuantity();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<PartnersResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _partnerRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _partnerRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailPartnerDTO>>(It.IsAny<IReadOnlyCollection<Partner>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetAllPartnersQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<PartnersResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _partnerRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailPartnerDTO>>(It.IsAny<IReadOnlyCollection<Partner>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_All_Partners_When_Quantity_Is_Null()
    {
        // arrange
        GetAllPartnersQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var partners = new List<Partner>
        {
            CreatePartner(companyId, "Parceiro 1"),
            CreatePartner(companyId, "Parceiro 2"),
            CreatePartner(companyId, "Parceiro 3"),
        };

        _partnerRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(partners);

        List<DetailPartnerDTO> mappedDtos = partners
            .Select(_ => default(DetailPartnerDTO)!)
            .ToList();

        _mapperMock
            .Setup(m => m.Map<List<DetailPartnerDTO>>(partners))
            .Returns(mappedDtos);

        // act
        Result<PartnersResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);

        var returnedDtos = result.Value.Partners;

        Assert.AreEqual(mappedDtos.Count, returnedDtos.Count);
        CollectionAssert.AreEquivalent(mappedDtos, returnedDtos.ToList());

        _partnerRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);
        _partnerRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailPartnerDTO>>(partners), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Call_GetAll_With_Quantity_When_Quantity_Is_Provided()
    {
        // arrange
        const int quantity = 2;
        GetAllPartnersQuery query = CreateQueryWithQuantity(quantity);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var partners = new List<Partner>
        {
            CreatePartner(companyId, "Parceiro 1"),
            CreatePartner(companyId, "Parceiro 2"),
        };

        _partnerRepositoryMock
            .Setup(r => r.GetAllAsync(quantity))
            .ReturnsAsync(partners);

        List<DetailPartnerDTO> mappedDtos = partners
            .Select(_ => default(DetailPartnerDTO)!)
            .ToList();

        _mapperMock
            .Setup(m => m.Map<List<DetailPartnerDTO>>(partners))
            .Returns(mappedDtos);

        // act
        Result<PartnersResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);

        var returnedDtos = result.Value.Partners;

        Assert.AreEqual(mappedDtos.Count, returnedDtos.Count);
        CollectionAssert.AreEquivalent(mappedDtos, returnedDtos.ToList());

        _partnerRepositoryMock.Verify(r =>
            r.GetAllAsync(quantity), Times.Once);
        _partnerRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailPartnerDTO>>(partners), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_And_LogError_When_Exception_Occurs()
    {
        // arrange
        GetAllPartnersQuery query = CreateQueryWithoutQuantity();

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
            .Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<PartnersResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a listagem de parceiros da empresa")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}