using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Application.RentalModule.Handlers;
using OblivionDrive.Application.RentalModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Tests.Unit.RentalModule.HandlerTests;

[TestClass]
[TestCategory("Rental - GetRentalByIdHandler Unit Tests")]
public class GetRentalByIdHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetRentalByIdHandler>> _loggerMock = default!;
    private Mock<IValidator<GetRentalByIdQuery>> _validatorMock = default!;
    private GetRentalByIdHandler _handler = default!;

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

        _rentalRepositoryMock = new Mock<IRepositoryRental>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<DetailRentalDTO>(It.IsAny<Rental>()))
            .Returns(default(DetailRentalDTO)!);

        _loggerMock = new Mock<ILogger<GetRentalByIdHandler>>();

        _validatorMock = new Mock<IValidator<GetRentalByIdQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetRentalByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetRentalByIdHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _rentalRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetRentalByIdQuery CreateValidQuery()
        => new(RentalId: Guid.NewGuid());

    private static User CreateCompanyUser(Guid id, Guid? companyId = null)
        => new()
        {
            Id = id,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? id
        };

    private static Rental CreateRental(Guid companyId)
        => new(
            companyId: companyId,
            clientId: Guid.NewGuid(),
            driverId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            planType: RentalPlanType.Daily,
            startDate: new DateOnly(2025, 1, 1),
            expectedReturnDate: new DateOnly(2025, 1, 2),
            insuranceDailyPricePerPerson: 0m,
            insurancePersonsCount: 0,
            estimatedTotalKilometers: 0,
            servicesTotalPrice: 0m,
            insuranceTotalPrice: 0m,
            rentalBasePrice: 0m,
            estimatedRentalAmount: 0m,
            serviceIds: null
        );

    [TestMethod]
    public async Task Handle_Should_Return_InvalidRequest_When_Validation_Fails()
    {
        // arrange
        GetRentalByIdQuery query = CreateValidQuery();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetRentalByIdQuery.RentalId), "O identificador do aluguel é obrigatório.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<DetailRentalDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m => m.Map<DetailRentalDTO>(It.IsAny<Rental>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetRentalByIdQuery query = CreateValidQuery();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<DetailRentalDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m => m.Map<DetailRentalDTO>(It.IsAny<Rental>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetRentalByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<DetailRentalDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m =>
            m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mapperMock.Verify(m => m.Map<DetailRentalDTO>(It.IsAny<Rental>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RecordNotFound_When_Rental_Does_Not_Exist()
    {
        // arrange
        GetRentalByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        User companyUser = CreateCompanyUser(currentUserId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _rentalRepositoryMock
            .Setup(r => r.GetByIdAsync(query.RentalId))
            .ReturnsAsync((Rental?)null);

        // act
        Result<DetailRentalDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.RentalId), Times.Once);

        _mapperMock.Verify(m => m.Map<DetailRentalDTO>(It.IsAny<Rental>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_Rental_Belongs_To_Other_Company()
    {
        // arrange
        GetRentalByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Rental rentalFromOtherCompany = CreateRental(otherCompanyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _rentalRepositoryMock
            .Setup(r => r.GetByIdAsync(query.RentalId))
            .ReturnsAsync(rentalFromOtherCompany);

        // act
        Result<DetailRentalDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r =>
            r.GetByIdAsync(query.RentalId), Times.Once);

        _mapperMock.Verify(m => m.Map<DetailRentalDTO>(It.IsAny<Rental>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_RentalDetail_When_Rental_Exists_And_Belongs_To_Current_Company()
    {
        // arrange
        GetRentalByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);
        Rental existingRental = CreateRental(companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _rentalRepositoryMock
            .Setup(r => r.GetByIdAsync(query.RentalId))
            .ReturnsAsync(existingRental);

        var expectedDetail = new DetailRentalDTO(
                Id: existingRental.Id,
                ClientId: existingRental.ClientId,
                DriverId: existingRental.DriverId,
                VehicleId: existingRental.VehicleId,
                PlanType: existingRental.PlanType,
                StartDate: existingRental.StartDate,
                ExpectedReturnDate: existingRental.ExpectedReturnDate,
                ActualReturnDate: existingRental.ActualReturnDate,
                EstimatedRentalAmount: existingRental.EstimatedRentalAmount,
                GrossRentalAmount: existingRental.GrossRentalAmount,
                FinalAmountToPay: existingRental.FinalAmountToPay,
                IsCompleted: existingRental.IsCompleted,
                CouponId: existingRental.CouponId,
                ServiceIds: existingRental.ServiceIds
            );

        _mapperMock
            .Setup(m => m.Map<DetailRentalDTO>(existingRental))
            .Returns(expectedDetail);


        _mapperMock
            .Setup(m => m.Map<DetailRentalDTO>(existingRental))
            .Returns(expectedDetail);

        // act
        Result<DetailRentalDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(expectedDetail, result.Value);

        _rentalRepositoryMock.Verify(r => r.GetByIdAsync(query.RentalId), Times.Once);
        _mapperMock.Verify(m => m.Map<DetailRentalDTO>(existingRental), Times.Once);
    }


    [TestMethod]
    public async Task Handle_Should_Return_Failure_And_LogError_When_Exception_Occurs()
    {
        // arrange
        GetRentalByIdQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        _rentalRepositoryMock
            .Setup(r => r.GetByIdAsync(query.RentalId))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<DetailRentalDTO> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a obtenção de detalhes do aluguel")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}