using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Application.RentalModule.Handlers;
using OblivionDrive.Application.RentalModule.Querys;
using OblivionDrive.Application.RentalModule.Results;
using OblivionDrive.Application.RentalModule.Services;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Tests.Unit.RentalModule.HandlerTests;

[TestClass]
[TestCategory("Rental - GenerateRentalPaymentsReportPdfHandler Unit Tests")]
public class GenerateRentalPaymentsReportPdfHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = default!;
    private Mock<ITenantProvider> _tenantProviderMock = default!;
    private Mock<IRepositoryRental> _rentalRepositoryMock = default!;
    private Mock<IRentalPaymentsReportPdfGenerator> _reportPdfGeneratorMock = default!;
    private Mock<ILogger<GenerateRentalPaymentsReportPdfHandler>> _loggerMock = default!;

    private GenerateRentalPaymentsReportPdfHandler _handler = default!;

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
        _tenantProviderMock.Setup(tp => tp.UserId).Returns(Guid.NewGuid());

        _rentalRepositoryMock = new Mock<IRepositoryRental>();

        _reportPdfGeneratorMock = new Mock<IRentalPaymentsReportPdfGenerator>();
        _reportPdfGeneratorMock
            .Setup(g => g.Generate(It.IsAny<RentalPaymentsReportPdfData>()))
            .Returns(Encoding.UTF8.GetBytes("pdf"));

        _loggerMock = new Mock<ILogger<GenerateRentalPaymentsReportPdfHandler>>();

        _handler = new GenerateRentalPaymentsReportPdfHandler(
            _userManagerMock.Object,
            _tenantProviderMock.Object,
            _rentalRepositoryMock.Object,
            _reportPdfGeneratorMock.Object,
            _loggerMock.Object
        );
    }

    private static GenerateRentalPaymentsReportPdfQuery CreateValidQuery()
        => new(Quantity: 10);

    private static User CreateCompanyUser(Guid userId, Guid? companyId = null)
        => new()
        {
            Id = userId,
            UserName = "companyUser",
            Email = "company@example.com",
            UserType = UserType.Company,
            CompanyId = companyId ?? userId
        };

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_UserId_Is_Null()
    {
        // arrange
        GenerateRentalPaymentsReportPdfQuery query = CreateValidQuery();
        _tenantProviderMock.Setup(tp => tp.UserId).Returns((Guid?)null);

        // act
        Result<PdfFileResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _rentalRepositoryMock.Verify(r => r.GetSummaryRowsByCompanyIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _reportPdfGeneratorMock.Verify(g => g.Generate(It.IsAny<RentalPaymentsReportPdfData>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_Return_Unauthorized_When_CurrentUser_Is_Not_Found()
    {
        // arrange
        GenerateRentalPaymentsReportPdfQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(currentUserId.ToString()))
            .ReturnsAsync((User?)null);

        // act
        Result<PdfFileResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _rentalRepositoryMock.Verify(r => r.GetSummaryRowsByCompanyIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _reportPdfGeneratorMock.Verify(g => g.Generate(It.IsAny<RentalPaymentsReportPdfData>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_Should_LogError_And_Return_Failure_When_Exception_Occurs()
    {
        // arrange
        GenerateRentalPaymentsReportPdfQuery query = CreateValidQuery();

        Guid currentUserId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _tenantProviderMock.Setup(tp => tp.UserId).Returns(currentUserId);
        _userManagerMock.Setup(m => m.FindByIdAsync(currentUserId.ToString())).ReturnsAsync(CreateCompanyUser(currentUserId, companyId));

        _rentalRepositoryMock
            .Setup(r => r.GetSummaryRowsByCompanyIdAsync(companyId, query.Quantity, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // act
        Result<PdfFileResult> result = await _handler.Handle(query, CancellationToken.None);

        // assert
        Assert.IsTrue(result.IsFailed);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Erro ao gerar relatório PDF de aluguéis")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}