using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Api.Models.ServicesModule;
using OblivionDrive.Api.Models.ServicesModule.Requests;
using OblivionDrive.Api.Models.ServicesModule.Responses;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.ServicesModule;
[TestClass]
[TestCategory("Services - API Integration Tests")]
public class ServiceIntegrationTests : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static ServiceIntegrationTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static Task<AccessToken?> ReadAccessTokenAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<AccessToken>(JsonOptions);

    private static Task<RegisterServiceResponse?> ReadRegisterServiceResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<RegisterServiceResponse>(JsonOptions);

    private static Task<UpdateServiceResponse?> ReadUpdateServiceResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<UpdateServiceResponse>(JsonOptions);

    private static Task<GetServiceByIdResponse?> ReadGetServiceByIdResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetServiceByIdResponse>(JsonOptions);

    private static Task<GetAllServicesResponse?> ReadGetAllServicesResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetAllServicesResponse>(JsonOptions);


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
            "Falha ao registrar usuário Company para o teste de serviços.");

        AccessToken? accessToken = await ReadAccessTokenAsync(response);
        Assert.IsNotNull(accessToken, "AccessToken não retornado pelo endpoint de registro.");

        return accessToken!;
    }

    private async Task<Guid> CreateServiceForCompanyAsync(AccessToken companyToken, string rawName, decimal price, ChargeType chargeType)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new RegisterServiceRequest(
            Name: rawName,
            Price: price,
            chargetype: chargeType
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/services", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Falha ao cadastrar o serviço usado no teste.");

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        string expectedFormattedName = NameFormatter.FormatName(rawName);

        Service? serviceFromDb = await dbContext.Services
            .SingleOrDefaultAsync(s => s.Name == expectedFormattedName && s.Price == price);

        Assert.IsNotNull(serviceFromDb, "Serviço não encontrado no banco após cadastro.");

        return serviceFromDb!.Id;
    }

    [TestMethod]
    public async Task RegisterService_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        var request = new RegisterServiceRequest(
            Name: "servico teste sem token",
            Price: 100m,
            chargetype: (ChargeType)1
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/services", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task RegisterService_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-company-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new RegisterServiceRequest(
            Name: string.Empty,
            Price: 0m,
            chargetype: (ChargeType)999
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/services", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);

        Assert.IsTrue(errors.Any(e => e.Contains("O nome do serviço é obrigatório.")),
            "Mensagem de erro esperada para Name não encontrada.");
        Assert.IsTrue(errors.Any(e => e.Contains("O preço do serviço deve ser maior que zero.")),
            "Mensagem de erro esperada para Price não encontrada.");
        Assert.IsTrue(errors.Any(e => e.Contains("O tipo de cobrança informado é inválido.")),
            "Mensagem de erro esperada para ChargeType não encontrada.");
    }

    [TestMethod]
    public async Task RegisterService_Should_Return_ServiceResponse_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-company-valid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string rawName = "servico de limpeza";
        decimal price = 150.50m;
        ChargeType chargeType = (ChargeType)1;

        var request = new RegisterServiceRequest(
            Name: rawName,
            Price: price,
            chargetype: chargeType 
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/services", request);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        RegisterServiceResponse? body =
            await ReadRegisterServiceResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.IsTrue(body!.CreatedSuccessfully);
        Assert.AreEqual(NameFormatter.FormatName(rawName), body.Name);
        Assert.AreEqual(price, body.Price);
        Assert.AreEqual(chargeType, body.ChargeType);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        string expectedName = NameFormatter.FormatName(rawName);

        Service? serviceFromDb = await dbContext.Services
            .SingleOrDefaultAsync(s => s.Name == expectedName && s.Price == price);

        Assert.IsNotNull(serviceFromDb);
        Assert.AreEqual(expectedName, serviceFromDb!.Name);
        Assert.AreEqual(price, serviceFromDb.Price);
        Assert.AreEqual(chargeType, serviceFromDb.ChargeType);
    }


    [TestMethod]
    public async Task UpdateService_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid serviceId = Guid.NewGuid();

        var request = new UpdateServiceRequest(
            Name: "servico atualizado",
            Price: 200m,
            ChargeType: (ChargeType)1
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/services/{serviceId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateService_Should_Return_BadRequest_When_ServiceId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-update-invalid-id@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        var request = new UpdateServiceRequest(
            Name: string.Empty,
            Price: 0m,
            ChargeType: (ChargeType)999
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/services/{emptyId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e => e.Contains("O identificador do serviço é obrigatório.")));
    }

    [TestMethod]
    public async Task UpdateService_Should_Return_NotFound_When_Service_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-update-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        var request = new UpdateServiceRequest(
            Name: "servico inexistente",
            Price: 123m,
            ChargeType: (ChargeType)1
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/services/{nonExistingId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateService_Should_Return_NotFound_When_Service_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-companyA-update@test.com",
            password: "Senha123!"
        );

        string rawName = "servico empresa A";
        decimal price = 100m;
        ChargeType chargeType = (ChargeType)1;

        Guid serviceId = await CreateServiceForCompanyAsync(
            companyAToken,
            rawName,
            price,
            chargeType
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-companyB-update@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        var request = new UpdateServiceRequest(
            Name: "tentativa indevida",
            Price: 200m,
            ChargeType: chargeType
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/services/{serviceId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateService_Should_Return_Updated_Service_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-update-ok@test.com",
            password: "Senha123!"
        );

        string initialRawName = "servico inicial";
        decimal initialPrice = 50m;
        ChargeType initialChargeType = (ChargeType)1;

        Guid serviceId = await CreateServiceForCompanyAsync(
            companyToken,
            initialRawName,
            initialPrice,
            initialChargeType
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string newRawName = "servico atualizado";
        decimal newPrice = 250.75m;
        ChargeType newChargeType = (ChargeType)1;

        var request = new UpdateServiceRequest(
            Name: newRawName,
            Price: newPrice,
            ChargeType: newChargeType
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/services/{serviceId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        UpdateServiceResponse? body = await ReadUpdateServiceResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.IsTrue(body!.UpdatedSuccessfully);
        Assert.AreEqual(NameFormatter.FormatName(newRawName), body.Name);
        Assert.AreEqual(newPrice, body.Price);
        Assert.AreEqual(newChargeType, body.ChargeType);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        Service? serviceFromDb = await dbContext.Services
            .SingleOrDefaultAsync(s => s.Id == serviceId);

        Assert.IsNotNull(serviceFromDb);

        string expectedFormattedName = NameFormatter.FormatName(newRawName);
        Assert.AreEqual(expectedFormattedName, serviceFromDb!.Name);
        Assert.AreEqual(newPrice, serviceFromDb.Price);
        Assert.AreEqual(newChargeType, serviceFromDb.ChargeType);
    }

    [TestMethod]
    public async Task DeleteService_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid serviceId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/services/{serviceId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteService_Should_Return_BadRequest_When_ServiceId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-delete-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/services/{emptyId}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e => e.Contains("O identificador do serviço é obrigatório.")));
    }

    [TestMethod]
    public async Task DeleteService_Should_Return_NotFound_When_Service_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-delete-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/services/{nonExistingId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteService_Should_Return_NotFound_When_Service_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-companyA-delete@test.com",
            password: "Senha123!"
        );

        string rawName = "servico empresa A para delete";
        decimal price = 100m;
        ChargeType chargeType = (ChargeType)1;

        Guid serviceId = await CreateServiceForCompanyAsync(
            companyAToken,
            rawName,
            price,
            chargeType
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-companyB-delete@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/services/{serviceId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetServiceById_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid serviceId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/services/{serviceId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetServiceById_Should_Return_BadRequest_When_ServiceId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-getbyid-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/services/{emptyId}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e => e.Contains("O identificador do serviço é obrigatório.")));
    }

    [TestMethod]
    public async Task GetServiceById_Should_Return_NotFound_When_Service_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-getbyid-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/services/{nonExistingId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetServiceById_Should_Return_NotFound_When_Service_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-companyA-getbyid@test.com",
            password: "Senha123!"
        );

        string rawName = "servico empresa A";
        decimal price = 123m;
        ChargeType chargeType = (ChargeType)1;

        Guid serviceId = await CreateServiceForCompanyAsync(
            companyAToken,
            rawName,
            price,
            chargeType
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-companyB-getbyid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/services/{serviceId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetServiceById_Should_Return_Service_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-getbyid-ok@test.com",
            password: "Senha123!"
        );

        string rawName = "servico get by id ok";
        decimal price = 250m;
        ChargeType chargeType = (ChargeType)1;

        Guid serviceId = await CreateServiceForCompanyAsync(
            companyToken,
            rawName,
            price,
            chargeType
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/services/{serviceId}");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetServiceByIdResponse? body =
            await ReadGetServiceByIdResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.AreEqual(serviceId, body!.Id);
        Assert.AreEqual(NameFormatter.FormatName(rawName), body.Name);
        Assert.AreEqual(price, body.Price);
        Assert.AreEqual(chargeType, body.ChargeType);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        Service? serviceFromDb = await dbContext.Services
            .SingleOrDefaultAsync(s => s.Id == serviceId);

        Assert.IsNotNull(serviceFromDb);

        string expectedName = NameFormatter.FormatName(rawName);
        Assert.AreEqual(expectedName, serviceFromDb!.Name);
        Assert.AreEqual(price, serviceFromDb.Price);
        Assert.AreEqual(chargeType, serviceFromDb.ChargeType);
    }

    [TestMethod]
    public async Task GetAllServices_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/services");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllServices_Should_Return_Empty_List_When_Company_Has_No_Services()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-getall-empty@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/services");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllServicesResponse? body =
            await ReadGetAllServicesResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.AreEqual(0, body!.Quantity);
        Assert.IsNotNull(body.Services);
        Assert.AreEqual(0, body.Services.Count);
    }

    [TestMethod]
    public async Task GetAllServices_Should_Return_All_Services_When_Quantity_Is_Null()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-getall-ok@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string[] rawNames =
        [
            "servico um",
        "servico dois",
        "servico tres"
        ];

        for (int i = 0; i < rawNames.Length; i++)
        {
            string rawName = rawNames[i];
            decimal price = 100m + i;

            var request = new RegisterServiceRequest(
                Name: rawName,
                Price: price,
                chargetype: (ChargeType)1
            );

            HttpResponseMessage registerResponse =
                await HttpClient.PostAsJsonAsync("/api/services", request);

            Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);
        }

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/services");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllServicesResponse? body =
            await ReadGetAllServicesResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.AreEqual(rawNames.Length, body!.Quantity);
        Assert.AreEqual(rawNames.Length, body.Services.Count);

        var expectedNames = rawNames
            .Select(NameFormatter.FormatName)
            .ToList();

        var actualNames = body.Services
            .Select(s => s.Name)
            .ToList();

        CollectionAssert.AreEquivalent(expectedNames, actualNames);
    }

    [TestMethod]
    public async Task GetAllServices_Should_Return_BadRequest_When_Quantity_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "services-getall-qty-zero@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        int invalidQuantity = 0;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/services?quantity={invalidQuantity}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
        Assert.IsTrue(
            errors.Any(e => e.Contains("A quantidade deve ser maior que zero.")),
            "Mensagem de validação esperada para quantity <= 0 não encontrada."
        );
    }
}