using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Api.Models.PartnerModule.Requests;
using OblivionDrive.Api.Models.PartnerModule.Responses;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.PartnerModule;
[TestClass]
[TestCategory("Partners - API Integration Tests")]
public class PartnerIntegrationTests : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static PartnerIntegrationTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static Task<AccessToken?> ReadAccessTokenAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<AccessToken>(JsonOptions);

    private static Task<RegisterPartnerResponse?> ReadRegisterPartnerResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<RegisterPartnerResponse>(JsonOptions);

    private static Task<UpdatePartnerResponse?> ReadUpdatePartnerResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<UpdatePartnerResponse>(JsonOptions);

    private static Task<DeletePartnerResponse?> ReadDeletePartnerResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<DeletePartnerResponse>(JsonOptions);

    private static Task<GetPartnerByIdResponse?> ReadGetPartnerByIdResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetPartnerByIdResponse>(JsonOptions);

    private static Task<GetAllPartnersResponse?> ReadGetAllPartnersResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetAllPartnersResponse>(JsonOptions);

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
            "Falha ao registrar usuário Company para o teste de parceiros.");

        AccessToken? accessToken = await ReadAccessTokenAsync(response);
        Assert.IsNotNull(accessToken, "AccessToken não retornado pelo endpoint de registro.");

        return accessToken!;
    }

    private async Task<Guid> GetPartnerIdByNameAsync(string expectedFormattedName)
    {
        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        Partner? partnerFromDb = await dbContext.Partners
            .SingleOrDefaultAsync(p => p.Name == expectedFormattedName);

        Assert.IsNotNull(partnerFromDb, $"Parceiro '{expectedFormattedName}' não encontrado no banco.");

        return partnerFromDb!.Id;
    }

    private async Task<Guid> CreatePartnerForCompanyAsync(AccessToken companyToken, string rawPartnerName)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new RegisterPartnerRequest(rawPartnerName);

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/partners", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Falha ao cadastrar o parceiro usado no teste.");

        RegisterPartnerResponse? body = await ReadRegisterPartnerResponseAsync(response);
        Assert.IsNotNull(body);
        Assert.IsTrue(body!.CreatedSuccessfully);

        string expectedFormattedName = NameFormatter.FormatName(rawPartnerName);
        Assert.AreEqual(expectedFormattedName, body.Name);

        return await GetPartnerIdByNameAsync(expectedFormattedName);
    }

    [TestMethod]
    public async Task RegisterPartner_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        var request = new RegisterPartnerRequest("parceiro sem token");

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/partners", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task RegisterPartner_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-company-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var invalidRequest = new RegisterPartnerRequest(string.Empty);

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/partners", invalidRequest);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);

        Assert.IsTrue(
            errors.Any(e =>
                e.Contains("parceiro", StringComparison.OrdinalIgnoreCase) &&
                e.Contains("obrigat", StringComparison.OrdinalIgnoreCase)),
            "Mensagem de validação esperada (nome obrigatório) não encontrada."
        );
    }

    [TestMethod]
    public async Task RegisterPartner_Should_Return_PartnerResponse_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-company-valid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string rawPartnerName = "parceiro teste";
        var request = new RegisterPartnerRequest(rawPartnerName);

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/partners", request);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        RegisterPartnerResponse? body = await ReadRegisterPartnerResponseAsync(response);
        Assert.IsNotNull(body);
        Assert.IsTrue(body!.CreatedSuccessfully);

        string expectedFormattedName = NameFormatter.FormatName(rawPartnerName);
        Assert.AreEqual(expectedFormattedName, body.Name);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        Partner? partnerFromDb = await dbContext.Partners
            .SingleOrDefaultAsync(p => p.Name == expectedFormattedName);

        Assert.IsNotNull(partnerFromDb);
        Assert.AreEqual(expectedFormattedName, partnerFromDb!.Name);
    }

    [TestMethod]
    public async Task UpdatePartner_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Guid partnerId = Guid.NewGuid();
        var request = new UpdatePartnerRequest("novo nome");

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/partners/{partnerId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdatePartner_Should_Return_BadRequest_When_PartnerId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-update-invalid-id@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;
        var request = new UpdatePartnerRequest("nome valido");

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/partners/{emptyId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdatePartner_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-update-invalid-request@test.com",
            password: "Senha123!"
        );

        Guid partnerId = await CreatePartnerForCompanyAsync(companyToken, "parceiro para update invalid");

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var invalidRequest = new UpdatePartnerRequest(string.Empty);

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/partners/{partnerId}", invalidRequest);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdatePartner_Should_Return_NotFound_When_Partner_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-update-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingPartnerId = Guid.NewGuid();
        var request = new UpdatePartnerRequest("nome qualquer");

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/partners/{nonExistingPartnerId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdatePartner_Should_Return_NotFound_When_Partner_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-companyA-update@test.com",
            password: "Senha123!"
        );

        Guid partnerId = await CreatePartnerForCompanyAsync(companyAToken, "parceiro empresa A");

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-companyB-update@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        var request = new UpdatePartnerRequest("tentativa indevida");

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/partners/{partnerId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdatePartner_Should_Return_UpdatedPartner_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-update-ok@test.com",
            password: "Senha123!"
        );

        Guid partnerId = await CreatePartnerForCompanyAsync(companyToken, "parceiro para atualizar");

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string newRawPartnerName = "parceiro atualizado";
        var request = new UpdatePartnerRequest(newRawPartnerName);

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/partners/{partnerId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        UpdatePartnerResponse? body = await ReadUpdatePartnerResponseAsync(response);
        Assert.IsNotNull(body);
        Assert.IsTrue(body!.UpdatedSuccessfully);

        string expectedFormattedName = NameFormatter.FormatName(newRawPartnerName);
        Assert.AreEqual(expectedFormattedName, body.Name);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        Partner? partnerFromDb = await dbContext.Partners
            .SingleOrDefaultAsync(p => p.Id == partnerId);

        Assert.IsNotNull(partnerFromDb);
        Assert.AreEqual(expectedFormattedName, partnerFromDb!.Name);
    }

    [TestMethod]
    public async Task DeletePartner_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid partnerId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/partners/{partnerId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeletePartner_Should_Return_BadRequest_When_PartnerId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-delete-invalid-id@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/partners/{emptyId}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeletePartner_Should_Return_NotFound_When_Partner_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-delete-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingPartnerId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/partners/{nonExistingPartnerId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeletePartner_Should_Return_NotFound_When_Partner_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-companyA-delete@test.com",
            password: "Senha123!"
        );

        Guid partnerId = await CreatePartnerForCompanyAsync(companyAToken, "parceiro empresa A delete");

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-companyB-delete@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/partners/{partnerId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeletePartner_Should_Delete_Partner_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-delete-ok@test.com",
            password: "Senha123!"
        );

        Guid partnerId = await CreatePartnerForCompanyAsync(companyToken, "parceiro para excluir");

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        Partner? partnerBeforeDelete = await dbContext.Partners.SingleOrDefaultAsync(p => p.Id == partnerId);
        Assert.IsNotNull(partnerBeforeDelete, "Parceiro não localizado no banco antes do delete.");

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/partners/{partnerId}");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        DeletePartnerResponse? body = await ReadDeletePartnerResponseAsync(response);
        Assert.IsNotNull(body);
        Assert.IsTrue(body!.DeletedSuccessfully);
        Assert.AreEqual(partnerId, body.PartnerId);

        dbContext.ChangeTracker.Clear();

        Partner? partnerAfterDelete = await dbContext.Partners.SingleOrDefaultAsync(p => p.Id == partnerId);
        Assert.IsNull(partnerAfterDelete, "Parceiro ainda existe no banco após exclusão.");
    }

    [TestMethod]
    public async Task GetPartnerById_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid partnerId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/partners/{partnerId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetPartnerById_Should_Return_BadRequest_When_PartnerId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-getbyid-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/partners/{emptyId}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetPartnerById_Should_Return_NotFound_When_Partner_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-getbyid-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingPartnerId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/partners/{nonExistingPartnerId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetPartnerById_Should_Return_NotFound_When_Partner_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-companyA-getbyid@test.com",
            password: "Senha123!"
        );

        string rawPartnerName = "parceiro empresa A getbyid";
        Guid partnerId = await CreatePartnerForCompanyAsync(companyAToken, rawPartnerName);

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-companyB-getbyid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/partners/{partnerId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetPartnerById_Should_Return_Partner_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-getbyid-ok@test.com",
            password: "Senha123!"
        );

        string rawPartnerName = "parceiro get by id ok";
        Guid partnerId = await CreatePartnerForCompanyAsync(companyToken, rawPartnerName);

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/partners/{partnerId}");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetPartnerByIdResponse? body = await ReadGetPartnerByIdResponseAsync(response);
        Assert.IsNotNull(body);

        string expectedFormattedName = NameFormatter.FormatName(rawPartnerName);
        Assert.AreEqual(partnerId, body!.Id);
        Assert.AreEqual(expectedFormattedName, body.Name);
    }

    [TestMethod]
    public async Task GetAllPartners_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/partners");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllPartners_Should_Return_Empty_List_When_Company_Has_No_Partners()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-getall-empty@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/partners");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllPartnersResponse? body = await ReadGetAllPartnersResponseAsync(response);

        Assert.IsNotNull(body);
        Assert.AreEqual(0, body!.Quantity);
        Assert.IsNotNull(body.Partners);
        Assert.AreEqual(0, body.Partners.Count);
    }

    [TestMethod]
    public async Task GetAllPartners_Should_Return_All_Partners_When_Quantity_Is_Null()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-getall-ok@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string[] rawPartnerNames =
        [
            "parceiro um",
            "parceiro dois",
            "parceiro tres"
        ];

        foreach (string rawName in rawPartnerNames)
        {
            await CreatePartnerForCompanyAsync(companyToken, rawName);
        }

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/partners");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllPartnersResponse? body = await ReadGetAllPartnersResponseAsync(response);

        Assert.IsNotNull(body);
        Assert.AreEqual(rawPartnerNames.Length, body!.Quantity);
        Assert.AreEqual(rawPartnerNames.Length, body.Partners.Count);

        var expectedNames = rawPartnerNames
            .Select(NameFormatter.FormatName)
            .ToList();

        var actualNames = body.Partners
            .Select(p => p.Name)
            .ToList();

        CollectionAssert.AreEquivalent(expectedNames, actualNames);
    }

    [TestMethod]
    public async Task GetAllPartners_Should_Return_BadRequest_When_Quantity_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "partners-getall-qty-zero@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        int invalidQuantity = 0;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/partners?quantity={invalidQuantity}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }
}