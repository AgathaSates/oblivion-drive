using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Api.Models.ClientModule.Requests;
using OblivionDrive.Api.Models.ClientModule.Responses;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.ClientModule;

[TestClass]
[TestCategory("Clients - API Integration Tests")]
public class ClientIntegrationTests : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static ClientIntegrationTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static Task<AccessToken?> ReadAccessTokenAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<AccessToken>(JsonOptions);

    private static Task<RegisterClientResponse?> ReadRegisterClientResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<RegisterClientResponse>(JsonOptions);

    private static Task<UpdateClientResponse?> ReadUpdateClientResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<UpdateClientResponse>(JsonOptions);

    private static Task<GetClientByIdResponse?> ReadGetClientByIdResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetClientByIdResponse>(JsonOptions);

    private static Task<GetAllClientsResponse?> ReadGetAllClientsResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetAllClientsResponse>(JsonOptions);

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
            "Falha ao registrar usuário Company para o teste de clientes.");

        AccessToken? accessToken = await ReadAccessTokenAsync(response);
        Assert.IsNotNull(accessToken, "AccessToken não retornado pelo endpoint de registro.");

        return accessToken!;
    }

    private async Task<Guid> GetClientIdByEmailAsync(string email)
    {
        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        var client = await dbContext.Set<OblivionDrive.Domain.ClientModule.Client>()
            .SingleOrDefaultAsync(c => c.Email == email);

        Assert.IsNotNull(client, $"Cliente com Email '{email}' não encontrado no banco para o teste.");

        return client!.Id;
    }

    private async Task<Guid> CreateClientForCompanyAsync(AccessToken companyToken, RegisterClientRequest request)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/clients", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Falha ao cadastrar o cliente usado no teste.");

        return await GetClientIdByEmailAsync(request.Email);
    }

    private static RegisterClientRequest CreateValidIndividualClientRequest(string rawName, string email) =>
        new(
            Name: rawName,
            Email: email,
            PhoneNumber: "11999999999",
            ClientType: ClientType.Individual,
            Cpf: "12345678901",
            Rg: "123456789",
            Cnh: "12345678901",
            Cnpj: null,
            State: "SC",
            City: "Florianopolis",
            District: "Centro",
            Street: "Rua A",
            Number: "100"
        );

    private static RegisterClientRequest CreateValidLegalEntityClientRequest(string rawName, string email) =>
        new(
            Name: rawName,
            Email: email,
            PhoneNumber: "11988888888",
            ClientType: ClientType.LegalEntity,
            Cpf: null,
            Rg: null,
            Cnh: null,
            Cnpj: "12345678000199",
            State: "SC",
            City: "Florianopolis",
            District: "Centro",
            Street: "Rua B",
            Number: "200"
        );

    [TestMethod]
    public async Task CreateClient_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        RegisterClientRequest request =
            CreateValidIndividualClientRequest("cliente sem token", "client.no-token@test.com");

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/clients", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateClient_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var invalidRequest = new RegisterClientRequest(
            Name: string.Empty,
            Email: "invalid-email",
            PhoneNumber: string.Empty,
            ClientType: (ClientType)999,
            Cpf: null,
            Rg: null,
            Cnh: null,
            Cnpj: null,
            State: string.Empty,
            City: string.Empty,
            District: string.Empty,
            Street: string.Empty,
            Number: string.Empty
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/clients", invalidRequest);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task CreateClient_Should_Return_ClientResponse_When_Request_Is_Valid_Individual()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-create-ok@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string rawName = "joao da silva";
        string email = "client.create-ok@test.com";

        RegisterClientRequest request =
            CreateValidIndividualClientRequest(rawName, email);

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/clients", request);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        RegisterClientResponse? body =
            await ReadRegisterClientResponseAsync(response);

        Assert.IsNotNull(body);
        Assert.IsTrue(body!.CreatedSuccessfully);

        string expectedName = NameFormatter.FormatName(rawName);
        Assert.AreEqual(expectedName, body.Name);
        Assert.AreEqual(request.Email, body.Email);
        Assert.AreEqual(request.PhoneNumber, body.PhoneNumber);
        Assert.AreEqual(request.ClientType, body.ClientType);
        Assert.AreEqual(request.Cpf, body.Cpf);
        Assert.AreEqual(request.Rg, body.Rg);
        Assert.AreEqual(request.Cnh, body.Cnh);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        var clientFromDb = await dbContext.Set<OblivionDrive.Domain.ClientModule.Client>()
            .SingleOrDefaultAsync(c => c.Email == email);

        Assert.IsNotNull(clientFromDb);
        Assert.AreEqual(expectedName, clientFromDb!.Name);
        Assert.AreEqual(email, clientFromDb.Email);
        Assert.AreEqual(request.PhoneNumber, clientFromDb.PhoneNumber);
        Assert.AreEqual(ClientType.Individual, clientFromDb.ClientType);
    }

    [TestMethod]
    public async Task UpdateClient_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Guid clientId = Guid.NewGuid();

        var request = new UpdateClientRequest(
            Name: "Novo Nome",
            Email: "novo@email.com",
            PhoneNumber: "11999999999",
            ClientType: ClientType.Individual,
            Cpf: "12345678901",
            Rg: "123456789",
            Cnh: "12345678901",
            Cnpj: null,
            State: "SC",
            City: "Florianopolis",
            District: "Centro",
            Street: "Rua A",
            Number: "100"
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/clients/{clientId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateClient_Should_Return_BadRequest_When_ClientId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-update-invalid-id@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        var request = new UpdateClientRequest(
            Name: string.Empty,
            Email: "invalid-email",
            PhoneNumber: string.Empty,
            ClientType: (ClientType)999,
            Cpf: null,
            Rg: null,
            Cnh: null,
            Cnpj: null,
            State: string.Empty,
            City: string.Empty,
            District: string.Empty,
            Street: string.Empty,
            Number: string.Empty
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/clients/{emptyId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateClient_Should_Return_NotFound_When_Client_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-update-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        var request = new UpdateClientRequest(
            Name: "Cliente Inexistente",
            Email: "client.notfound@test.com",
            PhoneNumber: "11999999999",
            ClientType: ClientType.Individual,
            Cpf: "12345678901",
            Rg: "123456789",
            Cnh: "12345678901",
            Cnpj: null,
            State: "SC",
            City: "Florianopolis",
            District: "Centro",
            Street: "Rua A",
            Number: "100"
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/clients/{nonExistingId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateClient_Should_Return_Updated_Client_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-update-ok@test.com",
            password: "Senha123!"
        );

        string initialEmail = "client.update-ok@test.com";
        Guid clientId = await CreateClientForCompanyAsync(
            companyToken,
            CreateValidIndividualClientRequest("cliente inicial", initialEmail)
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string newRawName = "cliente atualizado";
        string newEmail = "client.update-ok.new@test.com";

        var request = new UpdateClientRequest(
            Name: newRawName,
            Email: newEmail,
            PhoneNumber: "11977777777",
            ClientType: ClientType.Individual,
            Cpf: "12345678901",
            Rg: "987654321",
            Cnh: "10987654321",
            Cnpj: null,
            State: "SC",
            City: "Florianopolis",
            District: "Trindade",
            Street: "Rua Nova",
            Number: "999"
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/clients/{clientId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        UpdateClientResponse? body =
            await ReadUpdateClientResponseAsync(response);

        Assert.IsNotNull(body);
        Assert.IsTrue(body!.UpdatedSuccessfully);

        string expectedName = NameFormatter.FormatName(newRawName);

        Assert.AreEqual(expectedName, body.Name);
        Assert.AreEqual(newEmail, body.Email);
        Assert.AreEqual(request.PhoneNumber, body.PhoneNumber);
        Assert.AreEqual(request.ClientType, body.ClientType);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        var clientFromDb = await dbContext.Set<OblivionDrive.Domain.ClientModule.Client>()
            .SingleOrDefaultAsync(c => c.Id == clientId);

        Assert.IsNotNull(clientFromDb);
        Assert.AreEqual(expectedName, clientFromDb!.Name);
        Assert.AreEqual(newEmail, clientFromDb.Email);
        Assert.AreEqual(request.PhoneNumber, clientFromDb.PhoneNumber);
    }

    [TestMethod]
    public async Task DeleteClient_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid clientId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/clients/{clientId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteClient_Should_Return_BadRequest_When_ClientId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-delete-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/clients/{emptyId}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteClient_Should_Return_NotFound_When_Client_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-delete-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/clients/{nonExistingId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteClient_Should_Delete_Client_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-delete-ok@test.com",
            password: "Senha123!"
        );

        string email = "client.delete-ok@test.com";

        Guid clientId = await CreateClientForCompanyAsync(
            companyToken,
            CreateValidLegalEntityClientRequest("cliente para exclusao", email)
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        var beforeDelete = await dbContext.Set<OblivionDrive.Domain.ClientModule.Client>()
            .SingleOrDefaultAsync(c => c.Id == clientId);

        Assert.IsNotNull(beforeDelete, "Cliente não localizado no banco antes do delete.");

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/clients/{clientId}");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        DeleteClientResponse? body =
            await response.Content.ReadFromJsonAsync<DeleteClientResponse>(JsonOptions);

        Assert.IsNotNull(body);
        Assert.IsTrue(body!.DeletedSuccessfully);
        Assert.AreEqual(clientId, body.ClientId);

        dbContext.ChangeTracker.Clear();

        var afterDelete = await dbContext.Set<OblivionDrive.Domain.ClientModule.Client>()
            .SingleOrDefaultAsync(c => c.Id == clientId);

        Assert.IsNull(afterDelete, "Cliente ainda existe no banco após exclusão.");
    }

    [TestMethod]
    public async Task GetClientById_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid clientId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/clients/{clientId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetClientById_Should_Return_BadRequest_When_ClientId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-getbyid-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/clients/{emptyId}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetClientById_Should_Return_NotFound_When_Client_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-getbyid-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/clients/{nonExistingId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetClientById_Should_Return_Client_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-getbyid-ok@test.com",
            password: "Senha123!"
        );

        string rawName = "cliente get by id";
        string email = "client.getbyid-ok@test.com";

        Guid clientId = await CreateClientForCompanyAsync(
            companyToken,
            CreateValidIndividualClientRequest(rawName, email)
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/clients/{clientId}");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetClientByIdResponse? body =
            await ReadGetClientByIdResponseAsync(response);

        Assert.IsNotNull(body);

        string expectedName = NameFormatter.FormatName(rawName);

        Assert.AreEqual(clientId, body!.Id);
        Assert.AreEqual(expectedName, body.Name);
        Assert.AreEqual(email, body.Email);
        Assert.AreEqual(ClientType.Individual, body.ClientType);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        var clientFromDb = await dbContext.Set<OblivionDrive.Domain.ClientModule.Client>()
            .SingleOrDefaultAsync(c => c.Id == clientId);

        Assert.IsNotNull(clientFromDb);
        Assert.AreEqual(expectedName, clientFromDb!.Name);
        Assert.AreEqual(email, clientFromDb.Email);
    }

    [TestMethod]
    public async Task GetAllClients_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/clients");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllClients_Should_Return_Empty_List_When_Company_Has_No_Clients()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-getall-empty@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/clients");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllClientsResponse? body =
            await ReadGetAllClientsResponseAsync(response);

        Assert.IsNotNull(body);
        Assert.AreEqual(0, body!.Quantity);
        Assert.IsNotNull(body.Clients);
        Assert.AreEqual(0, body.Clients.Count);
    }

    [TestMethod]
    public async Task GetAllClients_Should_Return_BadRequest_When_Quantity_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "clients-company-getall-qty-zero@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        int invalidQuantity = 0;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/clients?quantity={invalidQuantity}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }
}
