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
[TestCategory("Coupon - GetAllCouponsHandler Unit Tests")]
public sealed class GetAllCouponsHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryCoupon> _couponRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetAllCouponsHandler>> _loggerMock = default!;
    private Mock<IValidator<GetAllCouponsQuery>> _validatorMock = default!;
    private GetAllCouponsHandler _handler = default!;

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

        _loggerMock = new Mock<ILogger<GetAllCouponsHandler>>();

        _validatorMock = new Mock<IValidator<GetAllCouponsQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetAllCouponsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetAllCouponsHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _couponRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetAllCouponsQuery CreateQueryWithoutQuantity()
        => new GetAllCouponsQuery(Quantity: null);

    private static GetAllCouponsQuery CreateQueryWithQuantity(int quantity)
        => new GetAllCouponsQuery(Quantity: quantity);

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

    private static Coupon CreateCoupon(Guid companyId, Guid partnerId, string name, decimal value)
    {
        return new Coupon(
            name: name,
            value: value,
            expirationDate: new DateOnly(2024, 12, 31),
            partnerId: partnerId,
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetAllCouponsQuery query = CreateQueryWithoutQuantity();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetAllCouponsQuery.Quantity), "A quantidade deve ser maior que zero.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<CouponsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailCouponDTO>>(It.IsAny<IReadOnlyCollection<Coupon>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetAllCouponsQuery query = CreateQueryWithoutQuantity();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<CouponsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailCouponDTO>>(It.IsAny<IReadOnlyCollection<Coupon>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetAllCouponsQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<CouponsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m =>
            m.Map<List<DetailCouponDTO>>(It.IsAny<IReadOnlyCollection<Coupon>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_All_Coupons_When_Quantity_Is_Null()
    {
        // arrange
        GetAllCouponsQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid partnerId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var coupons = new List<Coupon>
        {
            CreateCoupon(companyId, partnerId, "CUPOM10", 10m),
            CreateCoupon(companyId, partnerId, "CUPOM20", 20m),
            CreateCoupon(companyId, partnerId, "CUPOM30", 30m)
        };

        _couponRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(coupons);

        var detailList = new List<DetailCouponDTO>
        {
            new(coupons[0].Id, coupons[0].Name, coupons[0].Value, coupons[0].ExpirationDate, coupons[0].PartnerId),
            new(coupons[1].Id, coupons[1].Name, coupons[1].Value, coupons[1].ExpirationDate, coupons[1].PartnerId),
            new(coupons[2].Id, coupons[2].Name, coupons[2].Value, coupons[2].ExpirationDate, coupons[2].PartnerId)
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailCouponDTO>>(coupons))
            .Returns(detailList);

        // act
        Result<CouponsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.IsNotNull(result.Value.Coupons);
        Assert.AreEqual(detailList.Count, result.Value.Coupons.Count);
        CollectionAssert.AreEquivalent(detailList, result.Value.Coupons.ToList());

        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);
        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(It.IsAny<int>()), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailCouponDTO>>(coupons), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Call_GetAll_With_Quantity_When_Quantity_Is_Provided()
    {
        // arrange
        const int quantity = 2;
        GetAllCouponsQuery query = CreateQueryWithQuantity(quantity);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid partnerId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var coupons = new List<Coupon>
        {
            CreateCoupon(companyId, partnerId, "CUPOM10", 10m),
            CreateCoupon(companyId, partnerId, "CUPOM20", 20m)
        };

        _couponRepositoryMock
            .Setup(r => r.GetAllAsync(quantity))
            .ReturnsAsync(coupons);

        var detailList = new List<DetailCouponDTO>
        {
            new(coupons[0].Id, coupons[0].Name, coupons[0].Value, coupons[0].ExpirationDate, coupons[0].PartnerId),
            new(coupons[1].Id, coupons[1].Name, coupons[1].Value, coupons[1].ExpirationDate, coupons[1].PartnerId)
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailCouponDTO>>(coupons))
            .Returns(detailList);

        // act
        Result<CouponsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(detailList.Count, result.Value.Coupons.Count);

        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(quantity), Times.Once);
        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);

        _mapperMock.Verify(m =>
            m.Map<List<DetailCouponDTO>>(coupons), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Empty_Result_When_No_Coupons_Exist()
    {
        // arrange
        GetAllCouponsQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var emptyCoupons = new List<Coupon>();

        _couponRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(emptyCoupons);

        var emptyDetailList = new List<DetailCouponDTO>();

        _mapperMock
            .Setup(m => m.Map<List<DetailCouponDTO>>(emptyCoupons))
            .Returns(emptyDetailList);

        // act
        Result<CouponsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.IsNotNull(result.Value.Coupons);
        Assert.AreEqual(0, result.Value.Coupons.Count);

        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);
        _mapperMock.Verify(m =>
            m.Map<List<DetailCouponDTO>>(emptyCoupons), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_And_LogError_When_Exception_Occurs()
    {
        // arrange
        GetAllCouponsQuery query = CreateQueryWithoutQuantity();

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
            .Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<CouponsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a listagem de cupons da empresa")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
