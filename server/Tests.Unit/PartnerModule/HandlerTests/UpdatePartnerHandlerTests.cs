using System.Runtime.CompilerServices;
using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.PartnerModule.Commands;
using OblivionDrive.Application.PartnerModule.DTOs;
using OblivionDrive.Application.PartnerModule.Handlers;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Tests.Unit.PartnerModule.HandlerTests;

[TestClass]
[TestCategory("Partner - UpdatePartnerHandler Unit Tests")]
public class UpdatePartnerHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<IValidator<UpdatePartnerCommand>> _validatorMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryPartner> _partnerRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<ILogger<UpdatePartnerCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private UpdatePartnerHandler _handler = default!;

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

        _validatorMock = new Mock<IValidator<UpdatePartnerCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdatePartnerCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(Guid.NewGuid());

        _partnerRepositoryMock = new Mock<IRepositoryPartner>();

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<UpdatePartnerCommand>>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<UpdatedPartnerDTO>(It.IsAny<Partner>()))
            .Returns(CreateUninitialized<UpdatedPartnerDTO>());

        _handler = new UpdatePartnerHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _partnerRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    private static UpdatePartnerCommand CreateValidCommand()
        => new UpdatePartnerCommand(
            PartnerId: Guid.NewGuid(),
            Name: "parceiro atualizado"
        );

    private static User CreateCompanyUser(Guid id, Guid? companyId = null)
        => new User
        {
            Id = id,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? id
        };

    private static Partner CreatePartner(Guid companyId, Guid? partnerId = null, string? name = null)
    {
        Partner partner = new Partner(
            name: name ?? "Parceiro Original",
            companyId: companyId
        );

        if (partnerId.HasValue)
        {
            typeof(Partner).BaseType!.GetProperty("Id")!.SetValue(partner, partnerId.Value);
        }

        return partner;
    }

    private static T CreateUninitialized<T>() where T : class
        => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand();

        var failures = new List<ValidationFailure>
        {
            new(nameof(UpdatePartnerCommand.Name), "O nome do parceiro é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // act
        Result<UpdatedPartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _partnerRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _partnerRepositoryMock.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _partnerRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Partner>(), It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        _mapperMock.Verify(m => m.Map<UpdatedPartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<UpdatedPartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);

        _partnerRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _partnerRepositoryMock.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _partnerRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Partner>(), It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        _mapperMock.Verify(m => m.Map<UpdatedPartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<UpdatedPartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _partnerRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _partnerRepositoryMock.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _partnerRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Partner>(), It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        _mapperMock.Verify(m => m.Map<UpdatedPartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Partner_Does_Not_Exist()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand();

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
        Result<UpdatedPartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);

        _partnerRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Partner>(), It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        _mapperMock.Verify(m => m.Map<UpdatedPartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Partner_Belongs_To_Other_Company()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Partner partnerFromOtherCompany = CreatePartner(otherCompanyId, command.PartnerId);

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
        Result<UpdatedPartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _partnerRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.PartnerId), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);

        _partnerRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Partner>(), It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        _mapperMock.Verify(m => m.Map<UpdatedPartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Partner_Name_Already_Exists_For_Another_Partner()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Partner existingPartner = CreatePartner(companyId, command.PartnerId, "Parceiro Original");

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(existingPartner);

        string formattedName = NameFormatter.FormatName(command.Name);

        _partnerRepositoryMock
            .Setup(r => r.ExistsByNameAsync(formattedName, command.PartnerId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedPartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _partnerRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(formattedName, command.PartnerId), Times.Once);

        _partnerRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<Partner>(), It.IsAny<Partner>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);

        _mapperMock.Verify(m => m.Map<UpdatedPartnerDTO>(It.IsAny<Partner>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Update_Partner_And_Return_Success()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Partner existingPartner = CreatePartner(companyId, command.PartnerId, "Parceiro Original");

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(existingPartner);

        string expectedFormattedName = NameFormatter.FormatName(command.Name);

        _partnerRepositoryMock
            .Setup(r => r.ExistsByNameAsync(expectedFormattedName, command.PartnerId))
            .ReturnsAsync(false);

        Partner? capturedExisting = null;
        Partner? capturedUpdatedData = null;

        _partnerRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Partner>(), It.IsAny<Partner>()))
            .Callback<Partner, Partner>((existing, updatedData) =>
            {
                capturedExisting = existing;
                capturedUpdatedData = updatedData;

                existing.Update(updatedData);
            })
            .ReturnsAsync(() => capturedExisting!);

        UpdatedPartnerDTO expectedDto = CreateUninitialized<UpdatedPartnerDTO>();

        _mapperMock
            .Setup(m => m.Map<UpdatedPartnerDTO>(It.IsAny<Partner>()))
            .Returns(expectedDto);

        // act
        Result<UpdatedPartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(capturedUpdatedData);
        Assert.AreEqual(expectedFormattedName, capturedUpdatedData!.Name);
        Assert.AreEqual(existingPartner.CompanyId, capturedUpdatedData.CompanyId);

        Assert.IsNotNull(capturedExisting);
        Assert.AreEqual(command.PartnerId, capturedExisting!.Id);
        Assert.AreEqual(companyId, capturedExisting.CompanyId);
        Assert.AreEqual(expectedFormattedName, capturedExisting.Name);

        _partnerRepositoryMock.Verify(r => r.GetByIdAsync(command.PartnerId), Times.Once);
        _partnerRepositoryMock.Verify(r => r.ExistsByNameAsync(expectedFormattedName, command.PartnerId), Times.Once);
        _partnerRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Partner>(), It.IsAny<Partner>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Partner existingPartner = CreatePartner(companyId, command.PartnerId, "Parceiro Original");

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _partnerRepositoryMock
            .Setup(r => r.GetByIdAsync(command.PartnerId))
            .ReturnsAsync(existingPartner);

        string formattedName = NameFormatter.FormatName(command.Name);

        _partnerRepositoryMock
            .Setup(r => r.ExistsByNameAsync(formattedName, command.PartnerId))
            .ReturnsAsync(false);

        _partnerRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Partner>(), It.IsAny<Partner>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<UpdatedPartnerDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a atualização do parceiro")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}