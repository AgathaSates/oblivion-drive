using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using OblivionDrive.Application.CouponModule.Commands;
using OblivionDrive.Application.CouponModule.DTOs;
using OblivionDrive.Application.CouponModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.CouponModule.HandlerTests;

[TestClass]
[TestCategory("Coupon - RegisterCouponHandler Unit Tests")]
public class RegisterCouponHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<IValidator<RegisterCouponCommand>> _validatorMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryCoupon> _couponRepositoryMock = default!;
    private Mock<IRepositoryPartner> _partnerRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<RegisterCouponCommand>> _loggerMock = default!;
    private RegisterCouponHandler _handler = default!;

    [TestInitialize]
    public void Setup()
    {
        var userStore = new Mock<IUserStore<User>>();
        var identityOptions = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
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

        _validatorMock = new Mock<IValidator<RegisterCouponCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterCouponCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(Guid.NewGuid());

        _couponRepositoryMock = new Mock<IRepositoryCoupon>();
        _couponRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Coupon>()))
            .ReturnsAsync(Guid.NewGuid());

        _partnerRepositoryMock = new Mock<IRepositoryPartner>();

        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<CouponDTO>(It.IsAny<Coupon>()))
            .Returns(default(CouponDTO)!);

        _loggerMock = new Mock<ILogger<RegisterCouponCommand>>();

        _handler = new RegisterCouponHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _couponRepositoryMock.Object,
            _partnerRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    private static RegisterCouponCommand CreateValidCommand()
    {
        return new RegisterCouponCommand(
            Name: "SUMMER2024",
            Value: 50.00m,
            ExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            PartnerId: Guid.NewGuid()
        );
    }

    private static User CreateCompanyUser(Guid userId)
    {
        return new User
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = userId
        };
    }

    private static Partner CreatePartner(Guid partnerId, Guid companyId)
    {
        return new Partner("Partner Test", companyId)
        {
            Id = partnerId
        };
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        RegisterCouponCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(RegisterCouponCommand.Name), "O nome do cupom é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<CouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        RegisterCouponCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<CouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        RegisterCouponCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<CouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Partner_Does_Not_Exist()
    {
        // arrange
        RegisterCouponCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync((Partner?)null);

        // act
        Result<CouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Partner_Belongs_To_Different_Company()
    {
        // arrange
        RegisterCouponCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid differentCompanyId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Partner partner = CreatePartner(command.PartnerId, differentCompanyId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partner);

        // act
        Result<CouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Coupon_Name_Already_Exists()
    {
        // arrange
        RegisterCouponCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Partner partner = CreatePartner(command.PartnerId, currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partner);

        _couponRepositoryMock
            .Setup(r => r.ExistsByNameAsync(command.Name))
            .ReturnsAsync(true);

        // act
        Result<CouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(command.Name), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Success_When_Coupon_Is_Created()
    {
        // arrange
        RegisterCouponCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Partner partner = CreatePartner(command.PartnerId, currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partner);

        _couponRepositoryMock
            .Setup(r => r.ExistsByNameAsync(command.Name))
            .ReturnsAsync(false);

        var expectedCouponDto = new CouponDTO(
            true,
            command.Name,
            command.Value,
            command.ExpirationDate,
            command.PartnerId
        );

        _mapperMock
            .Setup(m => m.Map<CouponDTO>(It.IsAny<Coupon>()))
            .Returns(expectedCouponDto);

        // act
        Result<CouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(command.Name), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Coupon>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        RegisterCouponCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Partner partner = CreatePartner(command.PartnerId, currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partner);

        _couponRepositoryMock
            .Setup(r => r.ExistsByNameAsync(command.Name))
            .ReturnsAsync(false);

        _couponRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Coupon>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<CouponDTO> result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante o registro de cupom")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Create_Coupon_With_Correct_Data()
    {
        // arrange
        var command = CreateValidCommand();

        var currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        var companyUser = CreateCompanyUser(currentUserId);
        var partner = CreatePartner(command.PartnerId, currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partner);

        _couponRepositoryMock
            .Setup(r => r.ExistsByNameAsync(command.Name))
            .ReturnsAsync(false);

        Coupon? capturedCoupon = null;

        _couponRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Coupon>()))
            .Callback<Coupon>(c => capturedCoupon = c)
            .ReturnsAsync(Guid.NewGuid());

        var expectedDto = new CouponDTO(
            true,
            command.Name,
            command.Value,
            command.ExpirationDate,
            command.PartnerId
        );

        _mapperMock
            .Setup(m => m.Map<CouponDTO>(It.IsAny<Coupon>()))
            .Returns(expectedDto);

        // act
        Result<CouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert – resultado
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        // assert – cupom criado
        Assert.IsNotNull(capturedCoupon);
        Assert.AreEqual(command.Name, capturedCoupon!.Name);
        Assert.AreEqual(command.Value, capturedCoupon.Value);
        Assert.AreEqual(command.ExpirationDate, capturedCoupon.ExpirationDate);
        Assert.AreEqual(command.PartnerId, capturedCoupon.PartnerId);
        Assert.AreEqual(companyUser.CompanyId ?? companyUser.Id, capturedCoupon.CompanyId);
        Assert.AreNotEqual(Guid.Empty, capturedCoupon.Id);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(command.Name), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Coupon>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }
}
