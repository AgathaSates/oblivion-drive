using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Api.Models.VehicleGroupModule;
using OblivionDrive.Api.Models.VehicleGroupModule.Requests;
using OblivionDrive.Api.Models.VehicleGroupModule.Responses;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.VehicleGroupModule;

[TestClass]
[TestCategory("VehicleGroups - API Integration Tests")]
public class VehicleGroupIntegrationTests : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static VehicleGroupIntegrationTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static Task<AccessToken?> ReadAccessTokenAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<AccessToken>(JsonOptions);

    private static Task<RegisterVehicleGroupResponse?> ReadRegisterVehicleGroupResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<RegisterVehicleGroupResponse>(JsonOptions);

    private static Task<UpdateVehicleGroupResponse?> ReadUpdateVehicleGroupResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<UpdateVehicleGroupResponse>(JsonOptions);

    private static Task<GetVehicleGroupByIdResponse?> ReadGetVehicleGroupByIdResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetVehicleGroupByIdResponse>(JsonOptions);

    private static Task<GetAllVehicleGroupResponse?> ReadGetAllVehicleGroupResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetAllVehicleGroupResponse>(JsonOptions);

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
            "Falha ao registrar usuário Company para o teste de grupos de veículos.");

        AccessToken? accessToken = await ReadAccessTokenAsync(response);
        Assert.IsNotNull(accessToken, "AccessToken não retornado pelo endpoint de registro.");

        return accessToken!;
    }

    private async Task<Guid> CreateVehicleGroupForCompanyAsync(AccessToken companyToken, string rawName)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new RegisterVehicleGroupRequest(
            Name: rawName
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/vehicle-groups", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Falha ao cadastrar o grupo de veículos usado no teste.");

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        string expectedFormattedName = NameFormatter.FormatName(rawName);

        VehicleGroup? vehicleGroupFromDb = await dbContext.VehicleGroups
            .SingleOrDefaultAsync(vg => vg.Name == expectedFormattedName);

        Assert.IsNotNull(vehicleGroupFromDb, "Grupo de veículos não encontrado no banco após cadastro.");

        return vehicleGroupFromDb!.Id;
    }

    [TestMethod]
    public async Task RegisterVehicleGroup_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        var request = new RegisterVehicleGroupRequest(
            Name: "grupo sem token"
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/vehicle-groups", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task RegisterVehicleGroup_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-company-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new RegisterVehicleGroupRequest(
            Name: string.Empty
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/vehicle-groups", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);

        Assert.IsTrue(errors.Any(e => e.Contains("O nome do grupo de veículos é obrigatório.")),
            "Mensagem de erro esperada para Name não encontrada.");
    }

    [TestMethod]
    public async Task RegisterVehicleGroup_Should_Return_VehicleGroupResponse_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-company-valid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string rawName = "grupo de veiculos teste";

        var request = new RegisterVehicleGroupRequest(
            Name: rawName
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/vehicle-groups", request);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        RegisterVehicleGroupResponse? body =
            await ReadRegisterVehicleGroupResponseAsync(response);

        Assert.IsNotNull(body);

        string expectedName = NameFormatter.FormatName(rawName);

        Assert.IsTrue(body!.CreatedSuccessfully);
        Assert.AreEqual(expectedName, body.Name);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        VehicleGroup? vehicleGroupFromDb = await dbContext.VehicleGroups
            .SingleOrDefaultAsync(vg => vg.Name == expectedName);

        Assert.IsNotNull(vehicleGroupFromDb);
        Assert.AreEqual(expectedName, vehicleGroupFromDb!.Name);
    }

    // PUT /api/vehicle-groups/{id}

    [TestMethod]
    public async Task UpdateVehicleGroup_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid vehicleGroupId = Guid.NewGuid();

        var request = new UpdateVehicleGroupRequest(
            Name: "grupo atualizado"
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/vehicle-groups/{vehicleGroupId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateVehicleGroup_Should_Return_BadRequest_When_VehicleGroupId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-update-invalid-id@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        var request = new UpdateVehicleGroupRequest(
            Name: string.Empty
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/vehicle-groups/{emptyId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e => e.Contains("O identificador do grupo de veículos é obrigatório.")));
    }

    [TestMethod]
    public async Task UpdateVehicleGroup_Should_Return_NotFound_When_VehicleGroup_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-update-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        var request = new UpdateVehicleGroupRequest(
            Name: "grupo inexistente"
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/vehicle-groups/{nonExistingId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateVehicleGroup_Should_Return_NotFound_When_VehicleGroup_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-companyA-update@test.com",
            password: "Senha123!"
        );

        string rawName = "grupo empresa A";
        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyAToken,
            rawName
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-companyB-update@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        var request = new UpdateVehicleGroupRequest(
            Name: "tentativa indevida"
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/vehicle-groups/{vehicleGroupId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateVehicleGroup_Should_Return_Updated_VehicleGroup_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-update-ok@test.com",
            password: "Senha123!"
        );

        string initialRawName = "grupo inicial";

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            initialRawName
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string newRawName = "grupo atualizado";

        var request = new UpdateVehicleGroupRequest(
            Name: newRawName
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/vehicle-groups/{vehicleGroupId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        UpdateVehicleGroupResponse? body = await ReadUpdateVehicleGroupResponseAsync(response);

        Assert.IsNotNull(body);

        string expectedFormattedName = NameFormatter.FormatName(newRawName);

        Assert.IsTrue(body!.UpdatedSuccessfully);
        Assert.AreEqual(expectedFormattedName, body.Name);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        VehicleGroup? groupFromDb = await dbContext.VehicleGroups
            .SingleOrDefaultAsync(vg => vg.Id == vehicleGroupId);

        Assert.IsNotNull(groupFromDb);
        Assert.AreEqual(expectedFormattedName, groupFromDb!.Name);
    }

    [TestMethod]
    public async Task DeleteVehicleGroup_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid vehicleGroupId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/vehicle-groups/{vehicleGroupId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteVehicleGroup_Should_Return_BadRequest_When_VehicleGroupId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-delete-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/vehicle-groups/{emptyId}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e => e.Contains("O identificador do grupo de veículos é obrigatório.")));
    }

    [TestMethod]
    public async Task DeleteVehicleGroup_Should_Return_NotFound_When_VehicleGroup_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-delete-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/vehicle-groups/{nonExistingId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteVehicleGroup_Should_Return_NotFound_When_VehicleGroup_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-companyA-delete@test.com",
            password: "Senha123!"
        );

        string rawName = "grupo empresa A para delete";

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyAToken,
            rawName
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-companyB-delete@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/vehicle-groups/{vehicleGroupId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteVehicleGroup_Should_Delete_VehicleGroup_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-delete-ok@test.com",
            password: "Senha123!"
        );

        string rawName = "grupo para exclusao";

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            rawName
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        VehicleGroup? beforeDelete = await dbContext.VehicleGroups
            .SingleOrDefaultAsync(vg => vg.Id == vehicleGroupId);

        Assert.IsNotNull(beforeDelete, "Grupo de veículos não localizado no banco antes do delete.");

        // act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"/api/vehicle-groups/{vehicleGroupId}");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        DeleteVehicleGroupResponse? deleteResponse =
            await response.Content.ReadFromJsonAsync<DeleteVehicleGroupResponse>(JsonOptions);

        Assert.IsNotNull(deleteResponse);
        Assert.IsTrue(deleteResponse!.DeletedSuccessfully);
        Assert.AreEqual(vehicleGroupId, deleteResponse.VehicleGroupId);

        dbContext.ChangeTracker.Clear();

        VehicleGroup? afterDelete = await dbContext.VehicleGroups
            .SingleOrDefaultAsync(vg => vg.Id == vehicleGroupId);

        Assert.IsNull(afterDelete, "Grupo de veículos ainda existe no banco após exclusão.");
    }

    [TestMethod]
    public async Task GetVehicleGroupById_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid vehicleGroupId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicle-groups/{vehicleGroupId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetVehicleGroupById_Should_Return_BadRequest_When_VehicleGroupId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-getbyid-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicle-groups/{emptyId}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e => e.Contains("O identificador do grupo de veículos é obrigatório.")));
    }

    [TestMethod]
    public async Task GetVehicleGroupById_Should_Return_NotFound_When_VehicleGroup_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-getbyid-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicle-groups/{nonExistingId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetVehicleGroupById_Should_Return_NotFound_When_VehicleGroup_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-companyA-getbyid@test.com",
            password: "Senha123!"
        );

        string rawName = "grupo empresa A";

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyAToken,
            rawName
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-companyB-getbyid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicle-groups/{vehicleGroupId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetVehicleGroupById_Should_Return_VehicleGroup_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-getbyid-ok@test.com",
            password: "Senha123!"
        );

        string rawName = "grupo get by id ok";

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            rawName
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicle-groups/{vehicleGroupId}");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetVehicleGroupByIdResponse? body =
            await ReadGetVehicleGroupByIdResponseAsync(response);

        Assert.IsNotNull(body);

        string expectedName = NameFormatter.FormatName(rawName);

        Assert.AreEqual(vehicleGroupId, body!.Id);
        Assert.AreEqual(expectedName, body.Name);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        VehicleGroup? groupFromDb = await dbContext.VehicleGroups
            .SingleOrDefaultAsync(vg => vg.Id == vehicleGroupId);

        Assert.IsNotNull(groupFromDb);
        Assert.AreEqual(expectedName, groupFromDb!.Name);
    }

    [TestMethod]
    public async Task GetAllVehicleGroups_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/vehicle-groups");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllVehicleGroups_Should_Return_Empty_List_When_Company_Has_No_VehicleGroups()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-getall-empty@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/vehicle-groups");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllVehicleGroupResponse? body =
            await ReadGetAllVehicleGroupResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.AreEqual(0, body!.Quantity);
        Assert.IsNotNull(body.VehicleGroups);
        Assert.AreEqual(0, body.VehicleGroups.Count);
    }

    [TestMethod]
    public async Task GetAllVehicleGroups_Should_Return_All_VehicleGroups_When_Quantity_Is_Null()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-getall-ok@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string[] rawNames =
        [
            "grupo um",
            "grupo dois",
            "grupo tres"
        ];

        for (int i = 0; i < rawNames.Length; i++)
        {
            string rawName = rawNames[i];

            var request = new RegisterVehicleGroupRequest(
                Name: rawName
            );

            HttpResponseMessage registerResponse =
                await HttpClient.PostAsJsonAsync("/api/vehicle-groups", request);

            Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);
        }

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/vehicle-groups");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllVehicleGroupResponse? body =
            await ReadGetAllVehicleGroupResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.AreEqual(rawNames.Length, body!.Quantity);
        Assert.AreEqual(rawNames.Length, body.VehicleGroups.Count);

        var expectedNames = rawNames
            .Select(NameFormatter.FormatName)
            .ToList();

        var actualNames = body.VehicleGroups
            .Select(vg => vg.Name)
            .ToList();

        CollectionAssert.AreEquivalent(expectedNames, actualNames);
    }

    [TestMethod]
    public async Task GetAllVehicleGroups_Should_Return_BadRequest_When_Quantity_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehiclegroups-getall-qty-zero@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        int invalidQuantity = 0;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicle-groups?quantity={invalidQuantity}");

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