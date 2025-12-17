using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Api.Models.RentalModule.Requests;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.RentalModule;

[TestClass]
[TestCategory("Rentals - API Integration Tests")]
public sealed class RentalIntegrationTests : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static RentalIntegrationTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static string CreateUniqueEmail(string prefix) =>
        $"{prefix}.{Guid.NewGuid():N}@test.com";

    private static Task<AccessToken?> ReadAccessTokenAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<AccessToken>(JsonOptions);

    private static Task<List<string>?> ReadErrorsAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

    private async Task<AccessToken> RegisterCompanyAndGetTokenAsync(string userName, string password)
    {
        var request = new RegisterUserRequest(
            UserName: userName,
            Email: userName,
            Password: password
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Falha ao registrar usuário Company para o teste de aluguéis.");

        AccessToken? accessToken = await ReadAccessTokenAsync(response);
        Assert.IsNotNull(accessToken, "AccessToken não retornado pelo endpoint de registro.");

        return accessToken!;
    }

    private static RegisterRentalRequest CreateInvalidRegisterRequest() =>
        new(
            ClientId: Guid.Empty,
            DriverId: Guid.Empty,
            VehicleId: Guid.Empty,
            PlanType: (RentalPlanType)999,
            StartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            ExpectedReturnDate: DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            InsuranceDailyPricePerPerson: -1m,
            InsurancePersonsCount: 0,
            EstimatedTotalKilometers: -10,
            ServiceIds: Array.Empty<Guid>()
        );

    private static UpdateRentalRequest CreateInvalidUpdateRequest() =>
        new(
            ClientId: Guid.Empty,
            DriverId: Guid.Empty,
            VehicleId: Guid.Empty,
            PlanType: (RentalPlanType)999,
            StartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            ExpectedReturnDate: DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            InsuranceDailyPricePerPerson: -1m,
            InsurancePersonsCount: 0,
            EstimatedTotalKilometers: -10,
            ServiceIds: Array.Empty<Guid>()
        );

    private static CompleteRentalReturnRequest CreateInvalidCompleteReturnRequest() =>
        new(
            ActualReturnDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            InitialOdometerInKm: 1000,
            CurrentOdometerInKm: 900,
            IsFuelTankFullOnReturn: true,
            HasDamage: false,
            CouponName: null
        );

    [TestMethod]
    public async Task Create_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        RegisterRentalRequest request = CreateInvalidRegisterRequest();

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/rentals", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Update_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Guid rentalId = Guid.NewGuid();
        UpdateRentalRequest request = CreateInvalidUpdateRequest();

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/rentals/{rentalId}", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Delete_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Guid rentalId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/rentals/{rentalId}");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetById_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Guid rentalId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/rentals/{rentalId}");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAll_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/rentals");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task CompleteReturn_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Guid rentalId = Guid.NewGuid();
        CompleteRentalReturnRequest request = CreateInvalidCompleteReturnRequest();

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync($"/api/rentals/{rentalId}/return", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetReceiptPdf_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Guid rentalId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/rentals/{rentalId}/receipt");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task SendReceiptByEmail_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Guid rentalId = Guid.NewGuid();
        var request = new SendRentalReceiptEmailRequest("someone@test.com");

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync($"/api/rentals/{rentalId}/receipt/email", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetPaymentsReportPdf_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/rentals/report/payments");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ExportCsv_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/rentals/export/csv");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Create_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.create.invalid"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        RegisterRentalRequest request = CreateInvalidRegisterRequest();

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/rentals", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task Update_Should_Return_BadRequest_When_RentalId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.update.empty-id"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyRentalId = Guid.Empty;
        UpdateRentalRequest request = CreateInvalidUpdateRequest();

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/rentals/{emptyRentalId}", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task Delete_Should_Return_BadRequest_When_RentalId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.delete.empty-id"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyRentalId = Guid.Empty;

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/rentals/{emptyRentalId}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetById_Should_Return_BadRequest_When_RentalId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.getbyid.empty-id"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyRentalId = Guid.Empty;

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/rentals/{emptyRentalId}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task CompleteReturn_Should_Return_BadRequest_When_RentalId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.return.empty-id"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyRentalId = Guid.Empty;
        CompleteRentalReturnRequest request = CreateInvalidCompleteReturnRequest();

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync($"/api/rentals/{emptyRentalId}/return", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetAll_Should_Return_BadRequest_When_Quantity_Is_Less_Than_Or_Equal_To_Zero()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.getall.qty-zero"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        int invalidQuantity = 0;

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/rentals?quantity={invalidQuantity}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task Update_Should_Return_NotFound_When_Rental_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.update.notfound"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingRentalId = Guid.NewGuid();

        var request = new UpdateRentalRequest(
            ClientId: Guid.NewGuid(),
            DriverId: Guid.NewGuid(),
            VehicleId: Guid.NewGuid(),
            PlanType: RentalPlanType.Daily,
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            ExpectedReturnDate: DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            InsuranceDailyPricePerPerson: 10m,
            InsurancePersonsCount: 1,
            EstimatedTotalKilometers: 100,
            ServiceIds: null
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/rentals/{nonExistingRentalId}", request);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task Delete_Should_Return_NotFound_When_Rental_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.delete.notfound"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingRentalId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/rentals/{nonExistingRentalId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetById_Should_Return_NotFound_When_Rental_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.getbyid.notfound"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingRentalId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/rentals/{nonExistingRentalId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task CompleteReturn_Should_Return_NotFound_When_Rental_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.return.notfound"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingRentalId = Guid.NewGuid();

        var request = new CompleteRentalReturnRequest(
            ActualReturnDate: DateOnly.FromDateTime(DateTime.Today),
            InitialOdometerInKm: 1000,
            CurrentOdometerInKm: 1100,
            IsFuelTankFullOnReturn: true,
            HasDamage: false,
            CouponName: null
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync($"/api/rentals/{nonExistingRentalId}/return", request);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetReceiptPdf_Should_Return_NotFound_When_Rental_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.receipt.notfound"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingRentalId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/rentals/{nonExistingRentalId}/receipt");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task SendReceiptByEmail_Should_Return_NotFound_When_Rental_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.receipt-email.notfound"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingRentalId = Guid.NewGuid();
        var request = new SendRentalReceiptEmailRequest("someone@test.com");

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync($"/api/rentals/{nonExistingRentalId}/receipt/email", request);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetPaymentsReportPdf_Should_Return_Pdf_With_ContentDisposition_And_ExposeHeaders()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.report.pdf"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/rentals/report/payments");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/pdf", response.Content.Headers.ContentType?.MediaType);

        Assert.IsTrue(response.Headers.TryGetValues("Access-Control-Expose-Headers", out var exposedHeaders));
        CollectionAssert.Contains(exposedHeaders.ToList(), "Content-Disposition");

        Assert.IsNotNull(response.Content.Headers.ContentDisposition);
        Assert.IsTrue(!string.IsNullOrWhiteSpace(response.Content.Headers.ContentDisposition.FileNameStar)
                      || !string.IsNullOrWhiteSpace(response.Content.Headers.ContentDisposition.FileName));

        byte[] content = await response.Content.ReadAsByteArrayAsync();
        Assert.IsTrue(content.Length > 0, "PDF retornou vazio (esperado pelo menos um conteúdo mínimo).");
    }

    [TestMethod]
    public async Task ExportCsv_Should_Return_Csv_With_ContentDisposition_And_ExposeHeaders()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: CreateUniqueEmail("rentals.export.csv"),
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/rentals/export/csv");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/csv", response.Content.Headers.ContentType?.MediaType);

        Assert.IsTrue(response.Headers.TryGetValues("Access-Control-Expose-Headers", out var exposedHeaders));
        CollectionAssert.Contains(exposedHeaders.ToList(), "Content-Disposition");

        Assert.IsNotNull(response.Content.Headers.ContentDisposition);
        Assert.IsTrue(!string.IsNullOrWhiteSpace(response.Content.Headers.ContentDisposition.FileNameStar)
                      || !string.IsNullOrWhiteSpace(response.Content.Headers.ContentDisposition.FileName));

        byte[] content = await response.Content.ReadAsByteArrayAsync();
        Assert.IsTrue(content.Length > 0, "CSV retornou vazio (esperado pelo menos header/estrutura mínima).");
    }
}