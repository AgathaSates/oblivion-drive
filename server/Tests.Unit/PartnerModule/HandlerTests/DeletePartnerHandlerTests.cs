using System.Reflection;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.PartnerModule.Commands;
using OblivionDrive.Application.PartnerModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.PartnerModule.HandlerTests;

[TestClass]
[TestCategory("Partner - DeletePartnerHandler Unit Tests")]
public class DeletePartnerHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryPartner> _partnerRepositoryMock = default!;
    private Mock<IRepositoryCoupon> _couponRepositoryMock = default!;
    private Mock<IValidator<DeletePartnerCommand>> _validatorMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<DeletePartnerHandler>> _loggerMock = default!;
    private DeletePartnerHandler _handler = default!;

    [TestInitialize]
    public void Setup()
    {
        var userStoreMock = new Mock<IUserStore<User>>();
        IOptions<IdentityOptions> identityOptions = Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<User>>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>();
        var identityErrorDescriber = new IdentityErrorDescriber();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var userManagerLoggerMock = new Mock<ILogger<UserManager<User>>>();

        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object,
            identityOptions,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            lookupNormalizerMock.Object,
            identityErrorDescriber,
            serviceProviderMock.Object,
            userManagerLoggerMock.Object
        );

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(Guid.NewGuid());

        _partnerRepositoryMock = new Mock<IRepositoryPartner>();
        _partnerRepositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<Partner>()))
            .ReturnsAsync(true);

        _couponRepositoryMock = new Mock<IRepositoryCoupon>();
        _couponRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Coupon>());

        _validatorMock = new Mock<IValidator<DeletePartnerCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<DeletePartnerCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<DeletePartnerHandler>>();

        _handler = new DeletePartnerHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _partnerRepositoryMock.Object,
            _couponRepositoryMock.Object,
            _validatorMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private static DeletePartnerCommand CreateValidCommand()
        => new DeletePartnerCommand(PartnerId: Guid.NewGuid());

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
        => new User
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? userId
        };

    private static Partner CreatePartner(Guid companyId)
        => new Partner(name: "Parceiro para exclusão", companyId: companyId);

    private static Coupon CreateCouponWithPartnerId(Guid partnerId)
    {
        object? instance = Activator.CreateInstance(typeof(Coupon), nonPublic: true);

        if (instance is null)
            throw new InvalidOperationException(
                $"Não foi possível instanciar {nameof(Coupon)}. " +
                "Garanta um construtor parameterless (ao menos privado) para testes/EF.");

        var coupon = (Coupon)instance;

        PropertyInfo? partnerIdProperty = typeof(Coupon).GetProperty(nameof(Coupon.PartnerId));
        MethodInfo? nonPublicSetter = partnerIdProperty?.GetSetMethod(nonPublic: true);

        if (nonPublicSetter is not null)
        {
            nonPublicSetter.Invoke(coupon, new object[] { partnerId });
            return coupon;
        }

        FieldInfo? backingField = typeof(Coupon).GetField(
            "<PartnerId>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (backingField is not null)
        {
            backingField.SetValue(coupon, partnerId);
            return coupon;
        }

        throw new InvalidOperationException(
            $"Não foi possível atribuir {nameof(Coupon.PartnerId)} em {nameof(Coupon)}.");
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        DeletePartnerCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(DeletePartnerCommand.PartnerId), "O identificador do parceiro é obrigatório.")
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

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);

        _partnerRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        DeletePartnerCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);

        _partnerRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        DeletePartnerCommand command = CreateValidCommand();

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

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);

        _partnerRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Partner_Does_Not_Exist()
    {
        // arrange
        DeletePartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync((Partner?)null);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);

        _partnerRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Partner_Belongs_To_Other_Company()
    {
        // arrange
        DeletePartnerCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partnerFromOtherCompany);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Never);

        _partnerRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Partner_Has_Coupons()
    {
        // arrange
        DeletePartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Partner partner = CreatePartner(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partner);

        var coupons = new List<Coupon>
        {
            CreateCouponWithPartnerId(partner.Id)
        };

        _couponRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(coupons);

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.DeleteAsync(It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Delete_Partner_And_Return_Success()
    {
        // arrange
        DeletePartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Partner partner = CreatePartner(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partner);

        _couponRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Coupon>());

        // act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.GetAllAsync(), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.DeleteAsync(partner), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        DeletePartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Partner partner = CreatePartner(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partner);

        _couponRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Coupon>());

        _partnerRepositoryMock
            .Setup(r => r.DeleteAsync(partner))
            .ThrowsAsync(new Exception("Erro de banco"));

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
                    v.ToString()!.Contains("Ocorreu um erro durante a exclusão de parceiro")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}