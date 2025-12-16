using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Api.Models.CouponModule.Requests;
using OblivionDrive.Api.Models.CouponModule.Responses;
using OblivionDrive.Api.Models.PartnerModule.Requests;
using OblivionDrive.Api.Models.PartnerModule.Responses;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.CouponModule;

[TestClass]
[TestCategory("Coupons - API Integration Tests")]
public class CouponIntegrationTests : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static CouponIntegrationTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static Task<AccessToken?> ReadAccessTokenAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<AccessToken>(JsonOptions);

    private static Task<RegisterCouponResponse?> ReadRegisterCouponResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<RegisterCouponResponse>(JsonOptions);

    private static Task<UpdateCouponResponse?> ReadUpdateCouponResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<UpdateCouponResponse>(JsonOptions);

    private static Task<DeleteCouponResponse?> ReadDeleteCouponResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<DeleteCouponResponse>(JsonOptions);

    private static Task<GetCouponByIdResponse?> ReadGetCouponByIdResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetCouponByIdResponse>(JsonOptions);

    private static Task<GetAllCouponsResponse?> ReadGetAllCouponsResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetAllCouponsResponse>(JsonOptions);

    private static Task<List<string>?> ReadErrorsAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

    private async Task<AccessToken> RegisterCompanyAndGetTokenAsync(string userName, string password)
    {
        var registerUserRequest = new RegisterUserRequest(
            UserName: userName,
            Email: userName,
            Password: password
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/auth/register", registerUserRequest);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Falha ao registrar usuário Company para o teste de cupons.");

        AccessToken? accessToken = await ReadAccessTokenAsync(response);
        Assert.IsNotNull(accessToken, "AccessToken não retornado pelo endpoint de registro.");

        return accessToken!;
    }

    private async Task<Guid> CreatePartnerForCompanyAsync(AccessToken companyToken, string rawPartnerName)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var registerPartnerRequest = new RegisterPartnerRequest(rawPartnerName);

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/partners", registerPartnerRequest);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Falha ao cadastrar o parceiro usado no teste de cupons.");

        RegisterPartnerResponse? body =
            await response.Content.ReadFromJsonAsync<RegisterPartnerResponse>(JsonOptions);

        Assert.IsNotNull(body);
        Assert.IsTrue(body!.CreatedSuccessfully, "Parceiro não foi criado com sucesso.");

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        Partner? partnerFromDb = await dbContext.Partners
            .SingleOrDefaultAsync(p => p.Name == body.Name);

        Assert.IsNotNull(partnerFromDb, "Parceiro não encontrado no banco após cadastro.");

        return partnerFromDb!.Id;
    }

    private async Task<Guid> CreateCouponForCompanyAsync(
        AccessToken companyToken,
        string couponName,
        decimal couponValue,
        DateOnly expirationDate)
    {
        Guid partnerId = await CreatePartnerForCompanyAsync(
            companyToken,
            rawPartnerName: $"PARCEIRO"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var registerCouponRequest = new RegisterCouponRequest(
            Name: couponName,
            Value: couponValue,
            ExpirationDate: expirationDate,
            PartnerId: partnerId
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/coupons", registerCouponRequest);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Falha ao cadastrar o cupom usado no teste.");

        RegisterCouponResponse? responseBody = await ReadRegisterCouponResponseAsync(response);
        Assert.IsNotNull(responseBody);
        Assert.IsTrue(responseBody!.CreatedSuccessfully);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        Coupon? couponFromDb = await dbContext.Coupons
            .SingleOrDefaultAsync(c => c.Name == couponName && c.PartnerId == partnerId);

        Assert.IsNotNull(couponFromDb, "Cupom não encontrado no banco após cadastro.");

        return couponFromDb!.Id;
    }

    [TestMethod]
    public async Task RegisterCoupon_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        var request = new RegisterCouponRequest(
            Name: "CUPOM10",
            Value: 10m,
            ExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            PartnerId: Guid.NewGuid()
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/coupons", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task RegisterCoupon_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-company-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var invalidRequest = new RegisterCouponRequest(
            Name: "cupom invalido",
            Value: 0m,
            ExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            PartnerId: Guid.Empty
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/coupons", invalidRequest);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);

        Assert.IsTrue(errors.Any(e => e.Contains("nome do cupom", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(errors.Any(e => e.Contains("valor do cupom", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(errors.Any(e => e.Contains("validade", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(errors.Any(e => e.Contains("parceiro", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task UpdateCoupon_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Guid couponId = Guid.NewGuid();

        var request = new UpdateCouponRequest(
            Name: "CUPOM20",
            Value: 20m,
            ExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            PartnerId: Guid.NewGuid()
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/coupons/{couponId}", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateCoupon_Should_Return_BadRequest_When_CouponId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-update-empty-id@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyCouponId = Guid.Empty;

        var request = new UpdateCouponRequest(
            Name: "CUPOM20",
            Value: 20m,
            ExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            PartnerId: Guid.NewGuid()
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/coupons/{emptyCouponId}", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateCoupon_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-update-invalid@test.com",
            password: "Senha123!"
        );

        Guid couponId = await CreateCouponForCompanyAsync(
            companyToken,
            couponName: "CUPOM11",
            couponValue: 11m,
            expirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(15))
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var invalidRequest = new UpdateCouponRequest(
            Name: "cupom invalido",
            Value: -1m,
            ExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            PartnerId: Guid.Empty
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/coupons/{couponId}", invalidRequest);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateCoupon_Should_Return_NotFound_When_Coupon_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-update-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingCouponId = Guid.NewGuid();

        var request = new UpdateCouponRequest(
            Name: "CUPOM99",
            Value: 99m,
            ExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            PartnerId: Guid.NewGuid()
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/coupons/{nonExistingCouponId}", request);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateCoupon_Should_Return_NotFound_When_Coupon_Belongs_To_Other_Company()
    {
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-companyA-update@test.com",
            password: "Senha123!"
        );

        Guid couponId = await CreateCouponForCompanyAsync(
            companyAToken,
            couponName: "CUPOM21",
            couponValue: 21m,
            expirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(20))
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-companyB-update@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        var request = new UpdateCouponRequest(
            Name: "CUPOM22",
            Value: 22m,
            ExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(25)),
            PartnerId: Guid.NewGuid()
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/coupons/{couponId}", request);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteCoupon_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Guid couponId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/coupons/{couponId}");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteCoupon_Should_Return_BadRequest_When_CouponId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-delete-empty-id@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyCouponId = Guid.Empty;

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/coupons/{emptyCouponId}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteCoupon_Should_Return_NotFound_When_Coupon_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-delete-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingCouponId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/coupons/{nonExistingCouponId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteCoupon_Should_Return_NotFound_When_Coupon_Belongs_To_Other_Company()
    {
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-companyA-delete@test.com",
            password: "Senha123!"
        );

        Guid couponId = await CreateCouponForCompanyAsync(
            companyAToken,
            couponName: "CUPOM40",
            couponValue: 40m,
            expirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(20))
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-companyB-delete@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/coupons/{couponId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteCoupon_Should_Delete_Coupon_When_Request_Is_Valid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-delete-ok@test.com",
            password: "Senha123!"
        );

        Guid couponId = await CreateCouponForCompanyAsync(
            companyToken,
            couponName: "CUPOM50",
            couponValue: 50m,
            expirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(20))
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        Coupon? couponBeforeDelete = await dbContext.Coupons.SingleOrDefaultAsync(c => c.Id == couponId);
        Assert.IsNotNull(couponBeforeDelete, "Cupom não localizado no banco antes do delete.");

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/coupons/{couponId}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        DeleteCouponResponse? body = await ReadDeleteCouponResponseAsync(response);
        Assert.IsNotNull(body);

        Assert.IsTrue(body!.DeletedSuccessfully);
        Assert.AreEqual(couponId, body.CouponId);

        dbContext.ChangeTracker.Clear();

        Coupon? couponAfterDelete = await dbContext.Coupons.SingleOrDefaultAsync(c => c.Id == couponId);
        Assert.IsNull(couponAfterDelete, "Cupom ainda existe no banco após exclusão.");
    }

    // GET /api/coupons/{couponId}

    [TestMethod]
    public async Task GetCouponById_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Guid couponId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/coupons/{couponId}");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetCouponById_Should_Return_BadRequest_When_CouponId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-getbyid-empty-id@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyCouponId = Guid.Empty;

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/coupons/{emptyCouponId}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task GetCouponById_Should_Return_NotFound_When_Coupon_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-getbyid-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingCouponId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/coupons/{nonExistingCouponId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetCouponById_Should_Return_NotFound_When_Coupon_Belongs_To_Other_Company()
    {
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-companyA-getbyid@test.com",
            password: "Senha123!"
        );

        Guid couponId = await CreateCouponForCompanyAsync(
            companyAToken,
            couponName: "CUPOM60",
            couponValue: 60m,
            expirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-companyB-getbyid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/coupons/{couponId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetCouponById_Should_Return_Coupon_When_Request_Is_Valid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-getbyid-ok@test.com",
            password: "Senha123!"
        );

        string couponName = "CUPOM70";
        decimal couponValue = 70m;
        DateOnly expirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(25));

        Guid couponId = await CreateCouponForCompanyAsync(
            companyToken,
            couponName: couponName,
            couponValue: couponValue,
            expirationDate: expirationDate
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/coupons/{couponId}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetCouponByIdResponse? body = await ReadGetCouponByIdResponseAsync(response);
        Assert.IsNotNull(body);

        Assert.AreEqual(couponId, body!.Id);
        Assert.AreEqual(couponName, body.Name);
        Assert.AreEqual(couponValue, body.Value);
        Assert.AreEqual(expirationDate, body.ExpirationDate);
        Assert.AreNotEqual(Guid.Empty, body.PartnerId);
    }

    // GET /api/coupons?quantity={quantity}

    [TestMethod]
    public async Task GetAllCoupons_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/coupons");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllCoupons_Should_Return_Empty_List_When_Company_Has_No_Coupons()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-getall-empty@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/coupons");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllCouponsResponse? body = await ReadGetAllCouponsResponseAsync(response);

        Assert.IsNotNull(body);
        Assert.AreEqual(0, body!.Quantity);
        Assert.IsNotNull(body.Coupons);
        Assert.AreEqual(0, body.Coupons.Count);
    }

    [TestMethod]
    public async Task GetAllCoupons_Should_Return_BadRequest_When_Quantity_Is_Less_Than_Or_Equal_To_Zero()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "coupons-getall-qty-zero@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        int invalidQuantity = 0;

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/coupons?quantity={invalidQuantity}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }
}