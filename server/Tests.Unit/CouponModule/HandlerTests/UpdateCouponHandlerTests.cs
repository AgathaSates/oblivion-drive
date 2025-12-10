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
[TestCategory("Coupon - UpdateCouponHandler Unit Tests")]
public class UpdateCouponHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryCoupon> _couponRepositoryMock = default!;
    private Mock<IRepositoryPartner> _partnerRepositoryMock = default!;
    private Mock<IValidator<UpdateCouponCommand>> _validatorMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<UpdateCouponCommand>> _loggerMock = default!;
    private UpdateCouponHandler _handler = default!;

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

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(Guid.NewGuid());

        _couponRepositoryMock = new Mock<IRepositoryCoupon>();
        _couponRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<Coupon>()))
            .ReturnsAsync((Coupon entity, Coupon _) => entity);

        _partnerRepositoryMock = new Mock<IRepositoryPartner>();

        _validatorMock = new Mock<IValidator<UpdateCouponCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateCouponCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<UpdatedCouponDTO>(It.IsAny<Coupon>()))
            .Returns(default(UpdatedCouponDTO)!);

        _loggerMock = new Mock<ILogger<UpdateCouponCommand>>();

        _handler = new UpdateCouponHandler(
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

    private static UpdateCouponCommand CreateValidCommand()
    {
        return new UpdateCouponCommand(
            CouponId: Guid.NewGuid(),
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

    private static Coupon CreateCoupon(Guid couponId, Guid companyId, Guid partnerId)
    {
        return new Coupon(
            "ORIGINAL2024",
            25.00m,
            DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
            partnerId,
            companyId)
        {
            Id = couponId
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
        var command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(UpdateCouponCommand.Name), "O nome do cupom é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<UpdatedCouponDTO> result = await _handler.Handle(command, CancellationToken.None);

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
            r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        var command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<UpdatedCouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        var command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<UpdatedCouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Coupon_Is_Not_Found()
    {
        // arrange
        var command = CreateValidCommand();

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
        Result<UpdatedCouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.CouponId), Times.Once);
        _couponRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Coupon_Belongs_To_Other_Company()
    {
        // arrange
        var command = CreateValidCommand();

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
        Coupon couponFromOtherCompany = CreateCoupon(command.CouponId, otherCompanyId, command.PartnerId);

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(command.CouponId))
            .ReturnsAsync(couponFromOtherCompany);

        // act
        Result<UpdatedCouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);
        Assert.AreNotEqual(currentCompanyId, couponFromOtherCompany.CompanyId);

        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.CouponId), Times.Once);
        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Partner_Does_Not_Exist()
    {
        // arrange
        var command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Coupon existingCoupon = CreateCoupon(command.CouponId, currentCompanyId, command.PartnerId);

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(command.CouponId))
            .ReturnsAsync(existingCoupon);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync((Partner?)null);

        // act
        Result<UpdatedCouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.CouponId), Times.Once);
        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);
        _couponRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Partner_Belongs_To_Different_Company()
    {
        // arrange
        var command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Coupon existingCoupon = CreateCoupon(command.CouponId, currentCompanyId, Guid.NewGuid());

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(command.CouponId))
            .ReturnsAsync(existingCoupon);

        Guid differentCompanyId = Guid.NewGuid();
        Partner partnerFromDifferentCompany = CreatePartner(command.PartnerId, differentCompanyId);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partnerFromDifferentCompany);

        // act
        Result<UpdatedCouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _couponRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.CouponId), Times.Once);
        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);
        _couponRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _couponRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<Coupon>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Coupon_Name_Already_Exists()
    {
        // arrange
        var command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Coupon existingCoupon = CreateCoupon(command.CouponId, currentCompanyId, command.PartnerId);

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(command.CouponId))
            .ReturnsAsync(existingCoupon);

        Partner partner = CreatePartner(command.PartnerId, currentCompanyId);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partner);

        _couponRepositoryMock
            .Setup(r => r.ExistsByNameAsync(command.Name, command.CouponId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedCouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        var error = result.Errors.Single();

        Assert.IsTrue(
            error.Metadata.TryGetValue("ErrorType", out object? errorType) &&
            string.Equals(errorType?.ToString(), "InvalidRequest", StringComparison.Ordinal),
            "ErrorType deveria ser 'InvalidRequest'."
        );

        Assert.IsTrue(
            error.Reasons.Any(reason =>
                reason.Message.Contains("Já existe um cupom cadastrado com este nome", StringComparison.CurrentCulture)),
            "Deveria conter a mensagem de nome de cupom duplicado."
        );

        _couponRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(command.Name, command.CouponId), Times.Once);

        _couponRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<Coupon>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        var command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Coupon existingCoupon = CreateCoupon(command.CouponId, currentCompanyId, command.PartnerId);

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(command.CouponId))
            .ReturnsAsync(existingCoupon);

        Partner partner = CreatePartner(command.PartnerId, currentCompanyId);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partner);

        _couponRepositoryMock
            .Setup(r => r.ExistsByNameAsync(command.Name, command.CouponId))
            .ReturnsAsync(false);

        _couponRepositoryMock
            .Setup(r => r.UpdateAsync(existingCoupon, It.IsAny<Coupon>()))
            .ThrowsAsync(new Exception("Erro ao atualizar cupom"));

        // act
        Result<UpdatedCouponDTO> result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante a atualização do cupom")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Update_Coupon_With_Correct_Data_And_Return_Success()
    {
        // arrange
        var command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        User companyUser = CreateCompanyUser(currentUserId);
        Guid currentCompanyId = companyUser.CompanyId ?? companyUser.Id;

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        Coupon existingCoupon = CreateCoupon(command.CouponId, currentCompanyId, Guid.NewGuid());

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(command.CouponId))
            .ReturnsAsync(existingCoupon);

        Partner partner = CreatePartner(command.PartnerId, currentCompanyId);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(partner);

        _couponRepositoryMock
            .Setup(r => r.ExistsByNameAsync(command.Name, command.CouponId))
            .ReturnsAsync(false);

        Coupon? capturedUpdatedData = null;

        _couponRepositoryMock
            .Setup(r => r.UpdateAsync(existingCoupon, It.IsAny<Coupon>()))
            .Callback<Coupon, Coupon>((_, updated) => capturedUpdatedData = updated)
            .ReturnsAsync(existingCoupon);

        var expectedDto = new UpdatedCouponDTO(
            true,
            command.Name,
            command.Value,
            command.ExpirationDate,
            command.PartnerId
        );

        _mapperMock
            .Setup(m => m.Map<UpdatedCouponDTO>(existingCoupon))
            .Returns(expectedDto);

        // act
        Result<UpdatedCouponDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert 
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);
        Assert.IsTrue(result.Value.UpdatedSuccessfully);
        Assert.AreEqual(expectedDto.Name, result.Value.Name);
        Assert.AreEqual(expectedDto.Value, result.Value.Value);
        Assert.AreEqual(expectedDto.ExpirationDate, result.Value.ExpirationDate);
        Assert.AreEqual(expectedDto.PartnerId, result.Value.PartnerId);

        Assert.IsNotNull(capturedUpdatedData);
        Assert.AreEqual(command.Name, capturedUpdatedData!.Name);
        Assert.AreEqual(command.Value, capturedUpdatedData.Value);
        Assert.AreEqual(command.ExpirationDate, capturedUpdatedData.ExpirationDate);
        Assert.AreEqual(command.PartnerId, capturedUpdatedData.PartnerId);

        _couponRepositoryMock.Verify(r =>
            r.UpdateAsync(existingCoupon, It.IsAny<Coupon>()), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }
}
