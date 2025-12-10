using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.CouponModule.DTOs;
using OblivionDrive.Application.CouponModule.Handlers;
using OblivionDrive.Application.CouponModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.CouponModule;

namespace OblivionDrive.Tests.Unit.CouponModule.HandlerTests;

[TestClass]
[TestCategory("Coupon - GetCouponByIdHandler Unit Tests")]
public sealed class GetCouponByIdHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryCoupon> _couponRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetCouponByIdHandler>> _loggerMock = default!;
    private Mock<IValidator<GetCouponByIdQuery>> _validatorMock = default!;
    private GetCouponByIdHandler _handler = default!;

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

        _couponRepositoryMock = new Mock<IRepositoryCoupon>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<DetailCouponDTO>(It.IsAny<Coupon>()))
            .Returns(default(DetailCouponDTO)!);

        _loggerMock = new Mock<ILogger<GetCouponByIdHandler>>();

        _validatorMock = new Mock<IValidator<GetCouponByIdQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetCouponByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetCouponByIdHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _couponRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetCouponByIdQuery CreateValidQuery()
    {
        return new GetCouponByIdQuery(CouponId: Guid.NewGuid());
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

    private static Coupon CreateCoupon(Guid companyId, Guid partnerId)
    {
        return new Coupon(
            name: "CUPOM10",
            value: 50.00m,
            expirationDate: new DateOnly(2024, 12, 31),
            partnerId: partnerId,
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetCouponByIdQuery query = CreateValidQuery();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetCouponByIdQuery.CouponId), "O identificador do cupom é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<DetailCouponDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailCouponDTO>(It.IsAny<Coupon>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetCouponByIdQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<DetailCouponDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailCouponDTO>(It.IsAny<Coupon>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetCouponByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<DetailCouponDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<DetailCouponDTO>(It.IsAny<Coupon>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Coupon_Does_Not_Exist()
    {
        // arrange
        GetCouponByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(query.CouponId))
            .ReturnsAsync((Coupon?)null);

        // act
        Result<DetailCouponDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.CouponId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailCouponDTO>(It.IsAny<Coupon>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Coupon_Belongs_To_Other_Company()
    {
        // arrange
        GetCouponByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();
        Guid partnerId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Coupon couponFromOtherCompany = CreateCoupon(otherCompanyId, partnerId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(query.CouponId))
            .ReturnsAsync(couponFromOtherCompany);

        // act
        Result<DetailCouponDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.CouponId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailCouponDTO>(It.IsAny<Coupon>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_CouponDetail_When_Coupon_Exists_And_Belongs_To_Current_Company()
    {
        // arrange
        GetCouponByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid partnerId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Coupon existingCoupon = CreateCoupon(companyId, partnerId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(query.CouponId))
            .ReturnsAsync(existingCoupon);

        var expectedDetail = new DetailCouponDTO(
            Id: existingCoupon.Id,
            Name: existingCoupon.Name,
            Value: existingCoupon.Value,
            ExpirationDate: existingCoupon.ExpirationDate,
            PartnerId: existingCoupon.PartnerId
        );

        _mapperMock
            .Setup(m => m.Map<DetailCouponDTO>(existingCoupon))
            .Returns(expectedDetail);

        // act
        Result<DetailCouponDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDetail, result.Value);
        Assert.AreEqual(existingCoupon.Id, result.Value.Id);
        Assert.AreEqual(existingCoupon.Name, result.Value.Name);
        Assert.AreEqual(existingCoupon.Value, result.Value.Value);
        Assert.AreEqual(existingCoupon.ExpirationDate, result.Value.ExpirationDate);
        Assert.AreEqual(existingCoupon.PartnerId, result.Value.PartnerId);

        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.CouponId), Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailCouponDTO>(existingCoupon), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_And_LogError_When_Exception_Occurs()
    {
        // arrange
        GetCouponByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(query.CouponId))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<DetailCouponDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a obtenção de detalhes do cupom")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mapperMock.Verify(m =>
            m.Map<DetailCouponDTO>(It.IsAny<Coupon>()), Times.Never);
    }
}
