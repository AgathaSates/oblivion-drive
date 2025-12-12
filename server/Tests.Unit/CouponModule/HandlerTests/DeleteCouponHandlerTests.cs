using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.CouponModule.Commands;
using OblivionDrive.Application.CouponModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.CouponModule.HandlerTests;

[TestClass]
[TestCategory("Coupon - DeleteCouponHandler Unit Tests")]
public sealed class DeleteCouponHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryCoupon> _couponRepositoryMock = default!;
    private Mock<IValidator<DeleteCouponCommand>> _validatorMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<DeleteCouponHandler>> _loggerMock = default!;
    private DeleteCouponHandler _handler = default!;

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
        _couponRepositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<Coupon>()))
            .ReturnsAsync(true);

        _validatorMock = new Mock<IValidator<DeleteCouponCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<DeleteCouponCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<DeleteCouponHandler>>();

        _handler = new DeleteCouponHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _couponRepositoryMock.Object,
            _validatorMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private static DeleteCouponCommand CreateValidCommand()
    {
        return new DeleteCouponCommand(
            CouponId: Guid.NewGuid()
        );
    }

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
    {
        Guid effectiveCompanyId = companyId ?? userId;

        return new User
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = effectiveCompanyId
        };
    }

    private static Coupon CreateCoupon(Guid couponId, Guid companyId, Guid partnerId)
    {
        return new Coupon(
            name: "CUPOM10",
            value: 50.00m,
            expirationDate: new DateOnly(2024, 12, 31),
            partnerId: partnerId,
            companyId: companyId
        );
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        DeleteCouponCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(DeleteCouponCommand.CouponId),
                "O identificador do cupom é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        DeleteCouponCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        DeleteCouponCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Coupon_Is_Not_Found()
    {
        // arrange
        DeleteCouponCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(command.CouponId))
            .ReturnsAsync((Coupon?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.CouponId), Times.Once);
        _couponRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Coupon_Belongs_To_Other_Company()
    {
        // arrange
        DeleteCouponCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Guid otherCompanyId = Guid.NewGuid();
        Coupon couponFromOtherCompany = CreateCoupon(
            command.CouponId,
            otherCompanyId,
            Guid.NewGuid()
        );

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(command.CouponId))
            .ReturnsAsync(couponFromOtherCompany);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        Assert.AreNotEqual(currentCompanyId, couponFromOtherCompany.CompanyId);

        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.CouponId), Times.Once);
        _couponRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Delete_Coupon_And_Commit_When_All_Validations_Pass()
    {
        // arrange
        DeleteCouponCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Guid couponId = command.CouponId;
        Guid partnerId = Guid.NewGuid();

        Coupon coupon = CreateCoupon(couponId, currentCompanyId, partnerId);

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(couponId))
            .ReturnsAsync(coupon);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        _couponRepositoryMock.Verify(r =>
            r.DeleteAsync(coupon), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        DeleteCouponCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Guid couponId = command.CouponId;
        Guid partnerId = Guid.NewGuid();

        Coupon coupon = CreateCoupon(couponId, currentCompanyId, partnerId);

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(couponId))
            .ReturnsAsync(coupon);

        _couponRepositoryMock
            .Setup(r => r.DeleteAsync(coupon))
            .ThrowsAsync(new Exception("Erro de banco ao excluir cupom"));

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante a exclusão de cupom")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
