using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
[TestCategory("Rental - GetAllRentalsHandler Unit Tests")]
public class GetAllRentalsHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;
    private Mock<IMapper> _mapperMock = default!;
    private Mock<ILogger<GetAllRentalsHandler>> _loggerMock = default!;
    private Mock<IValidator<GetAllRentalsQuery>> _validatorMock = default!;
    private GetAllRentalsHandler _handler = default!;

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
        _loggerMock = new Mock<ILogger<GetAllRentalsHandler>>();

        _validatorMock = new Mock<IValidator<GetAllRentalsQuery>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<GetAllRentalsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _handler = new GetAllRentalsHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _rentalRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object
        );
    }

    private static GetAllRentalsQuery CreateQueryWithoutQuantity()
        => new(Quantity: null);

    private static GetAllRentalsQuery CreateQueryWithQuantity(int quantity)
        => new(Quantity: quantity);

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
        => new()
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? userId
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
        GetAllRentalsQuery query = CreateQueryWithoutQuantity();

        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(GetAllRentalsQuery.Quantity), "A quantidade deve ser maior que zero.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // act
        Result<RentalsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _validatorMock.Verify(v =>
            v.ValidateAsync(query, It.IsAny<CancellationToken>()), Times.Once);

        _tenantProviderMock.VerifyGet(tp => tp.UserId, Times.Never);
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetAllAsync(), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m => m.Map<List<DetailRentalDTO>>(It.IsAny<IReadOnlyCollection<Rental>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GetAllRentalsQuery query = CreateQueryWithoutQuantity();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns((Guid?)null);

        // act
        Result<RentalsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetAllAsync(), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m => m.Map<List<DetailRentalDTO>>(It.IsAny<IReadOnlyCollection<Rental>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GetAllRentalsQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<RentalsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(currentUserId.ToString()), Times.Once);

        _rentalRepositoryMock.Verify(r => r.GetAllAsync(), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<int>()), Times.Never);
        _mapperMock.Verify(m => m.Map<List<DetailRentalDTO>>(It.IsAny<IReadOnlyCollection<Rental>>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_All_Rentals_When_Quantity_Is_Null()
    {
        // arrange
        GetAllRentalsQuery query = CreateQueryWithoutQuantity();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var rentals = new List<Rental>
        {
            CreateRental(companyId),
            CreateRental(companyId),
            CreateRental(companyId),
        };

        _rentalRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(rentals);

        var mappedDtos = new List<DetailRentalDTO>
        {
            default!, default!, default!
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailRentalDTO>>(rentals))
            .Returns(mappedDtos);

        // act
        Result<RentalsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.IsNotNull(result.Value.Rentals);
        Assert.AreEqual(mappedDtos.Count, result.Value.Rentals.Count);

        _rentalRepositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
        _rentalRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<int>()), Times.Never);

        _mapperMock.Verify(m => m.Map<List<DetailRentalDTO>>(rentals), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Call_GetAll_With_Quantity_When_Quantity_Is_Provided()
    {
        // arrange
        const int quantity = 2;
        GetAllRentalsQuery query = CreateQueryWithQuantity(quantity);

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        User companyUser = CreateCompanyUser(currentUserId, companyId);

        _tenantProviderMock
            .Setup(tp => tp.UserId)
            .Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync(companyUser);

        var rentals = new List<Rental>
        {
            CreateRental(companyId),
            CreateRental(companyId),
        };

        _rentalRepositoryMock
            .Setup(r => r.GetAllAsync(quantity))
            .ReturnsAsync(rentals);

        var mappedDtos = new List<DetailRentalDTO>
        {
            default!, default!
        };

        _mapperMock
            .Setup(m => m.Map<List<DetailRentalDTO>>(rentals))
            .Returns(mappedDtos);

        // act
        Result<RentalsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.IsNotNull(result.Value.Rentals);
        Assert.AreEqual(mappedDtos.Count, result.Value.Rentals.Count);

        _rentalRepositoryMock.Verify(r => r.GetAllAsync(quantity), Times.Once);
        _rentalRepositoryMock.Verify(r => r.GetAllAsync(), Times.Never);

        _mapperMock.Verify(m => m.Map<List<DetailRentalDTO>>(rentals), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Failure_And_LogError_When_Exception_Occurs()
    {
        // arrange
        GetAllRentalsQuery query = CreateQueryWithoutQuantity();

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
            .Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<RentalsResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Ocorreu um erro durante a listagem de aluguéis da empresa")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}