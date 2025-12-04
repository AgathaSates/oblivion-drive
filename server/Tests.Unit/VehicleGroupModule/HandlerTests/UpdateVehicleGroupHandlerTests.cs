using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.Shared;
using OblivionDrive.Application.VehicleGroupModule.commands;
using OblivionDrive.Application.VehicleGroupModule.DTOs;
using OblivionDrive.Application.VehicleGroupModule.Handlers;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleGroupModule;

namespace OblivionDrive.Tests.Unit.VehicleGroupModule.HandlerTests;

[TestClass]
[TestCategory("VehicleGroup - UpdateVehicleGroupHandler Unit Tests")]
public class UpdateVehicleGroupHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryVehicleGroup> _vehicleGroupRepositoryMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IValidator<UpdateVehicleGroupCommand>> _validatorMock = default!;
    private Mock<ILogger<UpdateVehicleGroupCommand>> _loggerMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private UpdateVehicleGroupHandler _handler = default!;

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

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.RollbackAsync())
            .Returns(Task.CompletedTask);

        _validatorMock = new Mock<IValidator<UpdateVehicleGroupCommand>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateVehicleGroupCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _loggerMock = new Mock<ILogger<UpdateVehicleGroupCommand>>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<UpdatedVehicleGroupDTO>(It.IsAny<VehicleGroup>()))
            .Returns(default(UpdatedVehicleGroupDTO)!);

        _handler = new UpdateVehicleGroupHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _vehicleGroupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _mapperMock.Object
        );
    }

    private static UpdateVehicleGroupCommand CreateValidCommand()
    {
        return new UpdateVehicleGroupCommand(
            VehicleGroupId: Guid.NewGuid(),
            name: "grupo de veículos atualizado"
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
            name: "grupo original",
            companyId: companyId);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        UpdateVehicleGroupCommand command = CreateValidCommand();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(UpdateVehicleGroupCommand.name), "O nome do grupo de veículos é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<UpdatedVehicleGroupDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<VehicleGroup>(), It.IsAny<VehicleGroup>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        UpdateVehicleGroupCommand command = CreateValidCommand();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<UpdatedVehicleGroupDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<VehicleGroup>(), It.IsAny<VehicleGroup>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        UpdateVehicleGroupCommand command = CreateValidCommand();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<UpdatedVehicleGroupDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _vehicleGroupRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<VehicleGroup>(), It.IsAny<VehicleGroup>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_VehicleGroup_Does_Not_Exist()
    {
        // arrange
        UpdateVehicleGroupCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync((VehicleGroup?)null);

        // act
        Result<UpdatedVehicleGroupDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleGroupId), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<VehicleGroup>(), It.IsAny<VehicleGroup>()), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Update_VehicleGroup_And_Return_Success()
    {
        // arrange
        UpdateVehicleGroupCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(existingVehicleGroup);

        string expectedFormattedName = NameFormatter.FormatName(command.name);

        _vehicleGroupRepositoryMock
            .Setup(r => r.ExistsByNameAsync(expectedFormattedName, command.VehicleGroupId))
            .ReturnsAsync(false);

        VehicleGroup? capturedExisting = null;
        VehicleGroup? capturedUpdatedData = null;
        VehicleGroup updatedVehicleGroup = existingVehicleGroup;

        _vehicleGroupRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<VehicleGroup>(), It.IsAny<VehicleGroup>()))
            .Callback<VehicleGroup, VehicleGroup>((existing, updatedData) =>
            {
                capturedExisting = existing;
                capturedUpdatedData = updatedData;

                existing.Update(updatedData);
                updatedVehicleGroup = existing;
            })
            .ReturnsAsync(() => updatedVehicleGroup);

        UpdatedVehicleGroupDTO expectedDto = new(
            UpdatedSuccessfully: true,
            Name: expectedFormattedName);

        _mapperMock
            .Setup(m => m.Map<UpdatedVehicleGroupDTO>(updatedVehicleGroup))
            .Returns(expectedDto);

        // act
        Result<UpdatedVehicleGroupDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDto, result.Value);

        Assert.IsNotNull(capturedUpdatedData);
        Assert.AreEqual(expectedFormattedName, capturedUpdatedData!.Name);
        Assert.AreEqual(existingVehicleGroup.CompanyId, capturedUpdatedData.CompanyId);

        Assert.IsNotNull(capturedExisting);
        Assert.AreEqual(expectedFormattedName, capturedExisting!.Name);
        Assert.AreEqual(companyId, capturedExisting.CompanyId);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.GetByIdAsync(command.VehicleGroupId), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(expectedFormattedName, command.VehicleGroupId), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<VehicleGroup>(), It.IsAny<VehicleGroup>()), Times.Once);

        _unitOfWorkMock.Verify(u =>
            u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u =>
            u.RollbackAsync(), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Rollback_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        UpdateVehicleGroupCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(existingVehicleGroup);

        string formattedName = NameFormatter.FormatName(command.name);

        _vehicleGroupRepositoryMock
            .Setup(r => r.ExistsByNameAsync(formattedName, command.VehicleGroupId))
            .ReturnsAsync(false);

        _vehicleGroupRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<VehicleGroup>(), It.IsAny<VehicleGroup>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<UpdatedVehicleGroupDTO> result = await _handler.Handle(command, CancellationToken.None);

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
                    v.ToString()!.Contains("Ocorreu um erro durante a atualização do grupo de veículos")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_VehicleGroup_Name_Already_Exists()
    {
        // arrange
        UpdateVehicleGroupCommand command = CreateValidCommand();

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
            .Setup(r => r.GetByIdAsync(command.VehicleGroupId))
            .ReturnsAsync(existingVehicleGroup);

        string formattedName = NameFormatter.FormatName(command.name);

        _vehicleGroupRepositoryMock
            .Setup(r => r.ExistsByNameAsync(formattedName, command.VehicleGroupId))
            .ReturnsAsync(true);

        // act
        Result<UpdatedVehicleGroupDTO> result = await _handler.Handle(command, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed, "Resultado deveria ser falha quando o nome do grupo de veículos já existe para outro registro.");

        var error = result.Errors.Single();

        Assert.IsTrue(
            error.Metadata.TryGetValue("ErrorType", out object? errorType) &&
            string.Equals(errorType?.ToString(), "InvalidRequest", StringComparison.Ordinal),
            "ErrorType deveria ser 'InvalidRequest'."
        );

        Assert.IsTrue(
            error.Reasons.Any(r =>
                r.Message.Contains("Já existe um grupo de veículos cadastrado com este nome para esta empresa.",
                    StringComparison.CurrentCulture)),
            "Mensagem de erro deveria indicar que já existe um grupo de veículos cadastrado com este nome para esta empresa."
        );

        _vehicleGroupRepositoryMock.Verify(r =>
            r.ExistsByNameAsync(formattedName, command.VehicleGroupId), Times.Once);

        _vehicleGroupRepositoryMock.Verify(r =>
            r.UpdateAsync(It.IsAny<VehicleGroup>(), It.IsAny<VehicleGroup>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }
}