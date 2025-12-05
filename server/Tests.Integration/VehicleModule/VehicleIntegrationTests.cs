using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Api.Models.VehicleGroupModule;
using OblivionDrive.Api.Models.VehicleModule.Responses;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Domain.VehicleModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.VehicleModule;

[TestClass]
[TestCategory("Vehicles - API Integration Tests")]
public class VehicleIntegrationTests : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly byte[] DefaultPhotoBytes = { 1, 2, 3, 4, 5 };

    static VehicleIntegrationTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static Task<AccessToken?> ReadAccessTokenAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<AccessToken>(JsonOptions);

    private static Task<RegisterVehicleResponse?> ReadRegisterVehicleResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<RegisterVehicleResponse>(JsonOptions);

    private static Task<UpdateVehicleResponse?> ReadUpdateVehicleResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<UpdateVehicleResponse>(JsonOptions);

    private static Task<GetVehicleByIdResponse?> ReadGetVehicleByIdResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetVehicleByIdResponse>(JsonOptions);

    private static Task<GetAllVehiclesResponse?> ReadGetAllVehiclesResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetAllVehiclesResponse>(JsonOptions);

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
            "Falha ao registrar usuário Company para o teste de veículos.");

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

    private async Task<Guid> CreateVehicleForCompanyAsync(
        AccessToken companyToken,
        Guid vehicleGroupId,
        string licensePlate,
        string brand,
        string model)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new RegisterVehicleRequest(
            LicensePlate: licensePlate,
            Brand: brand,
            Model: model,
            Color: "Preto",
            FuelType: (FuelType)1,
            FuelTankCapacityInLiters: 50m,
            Year: 2020,
            VehicleGroupId: vehicleGroupId,
            PhotoBytes: DefaultPhotoBytes
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/vehicles", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Falha ao cadastrar o veículo usado no teste.");

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        Vehicle? vehicleFromDb = await dbContext.Vehicles
            .SingleOrDefaultAsync(v =>
                v.LicensePlate == licensePlate &&
                v.VehicleGroupId == vehicleGroupId);

        Assert.IsNotNull(vehicleFromDb, "Veículo não encontrado no banco após cadastro.");

        return vehicleFromDb!.Id;
    }

    [TestMethod]
    public async Task RegisterVehicle_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        var request = new RegisterVehicleRequest(
            LicensePlate: "AAA1234",
            Brand: "Fiat",
            Model: "Uno",
            Color: "Preto",
            FuelType: (FuelType)1,
            FuelTankCapacityInLiters: 50m,
            Year: 2020,
            VehicleGroupId: Guid.NewGuid(),
            PhotoBytes: DefaultPhotoBytes
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/vehicles", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task RegisterVehicle_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-company-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new RegisterVehicleRequest(
            LicensePlate: string.Empty,
            Brand: string.Empty,
            Model: string.Empty,
            Color: string.Empty,
            FuelType: (FuelType)999,
            FuelTankCapacityInLiters: 0m,
            Year: 0,
            VehicleGroupId: Guid.Empty,
            PhotoBytes: Array.Empty<byte>()
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/vehicles", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task RegisterVehicle_Should_Return_VehicleResponse_When_Request_Is_Valid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-company-valid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            rawName: "grupo para cadastro de veiculo"
        );

        string licensePlate = "ABC1234";
        string brand = "Fiat";
        string model = "Uno";

        var request = new RegisterVehicleRequest(
            LicensePlate: licensePlate,
            Brand: brand,
            Model: model,
            Color: "Preto",
            FuelType: (FuelType)1,
            FuelTankCapacityInLiters: 50m,
            Year: 2020,
            VehicleGroupId: vehicleGroupId,
            PhotoBytes: DefaultPhotoBytes
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/vehicles", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        RegisterVehicleResponse? body =
            await ReadRegisterVehicleResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.IsTrue(body!.CreatedSuccessfully);
        Assert.AreEqual(licensePlate, body.LicensePlate);
        Assert.AreEqual(brand, body.Brand);
        Assert.AreEqual(model, body.Model);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        Vehicle? vehicleFromDb = await dbContext.Vehicles
            .SingleOrDefaultAsync(v =>
                v.LicensePlate == licensePlate &&
                v.VehicleGroupId == vehicleGroupId);

        Assert.IsNotNull(vehicleFromDb);
    }

    [TestMethod]
    public async Task UpdateVehicle_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid vehicleId = Guid.NewGuid();

        var request = new UpdateVehicleRequest(
            VehicleId: vehicleId,
            Brand: "Fiat",
            Model: "Uno",
            Color: "Prata",
            FuelType: (FuelType)1,
            FuelTankCapacityInLiters: 50m,
            Year: 2020,
            VehicleGroupId: Guid.NewGuid(),
            PhotoBytes: null
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/vehicles/{vehicleId}", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateVehicle_Should_Return_BadRequest_When_VehicleId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-update-invalid-id@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        var request = new UpdateVehicleRequest(
            VehicleId: emptyId,
            Brand: string.Empty,
            Model: string.Empty,
            Color: string.Empty,
            FuelType: (FuelType)999,
            FuelTankCapacityInLiters: 0m,
            Year: 0,
            VehicleGroupId: Guid.Empty,
            PhotoBytes: null
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/vehicles/{emptyId}", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateVehicle_Should_Return_NotFound_When_Vehicle_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-update-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        var request = new UpdateVehicleRequest(
            VehicleId: nonExistingId,
            Brand: "Fiat",
            Model: "Uno",
            Color: "Prata",
            FuelType: (FuelType)1,
            FuelTankCapacityInLiters: 50m,
            Year: 2020,
            VehicleGroupId: Guid.NewGuid(),
            PhotoBytes: null
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/vehicles/{nonExistingId}", request);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateVehicle_Should_Return_NotFound_When_Vehicle_Belongs_To_Other_Company()
    {
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-companyA-update@test.com",
            password: "Senha123!"
        );

        Guid groupAId = await CreateVehicleGroupForCompanyAsync(
            companyAToken,
            rawName: "grupo empresa A"
        );

        Guid vehicleId = await CreateVehicleForCompanyAsync(
            companyAToken,
            groupAId,
            licensePlate: "ABC1234",
            brand: "Fiat",
            model: "Uno"
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-companyB-update@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        var request = new UpdateVehicleRequest(
            VehicleId: vehicleId,
            Brand: "VW",
            Model: "Gol",
            Color: "Preto",
            FuelType: (FuelType)1,
            FuelTankCapacityInLiters: 55m,
            Year: 2021,
            VehicleGroupId: groupAId,
            PhotoBytes: null
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/vehicles/{vehicleId}", request);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors!.Count > 0);
    }

    [TestMethod]
    public async Task UpdateVehicle_Should_Return_Updated_Vehicle_When_Request_Is_Valid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-update-ok@test.com",
            password: "Senha123!"
        );

        Guid groupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            rawName: "grupo update veiculo"
        );

        Guid vehicleId = await CreateVehicleForCompanyAsync(
            companyToken,
            groupId,
            licensePlate: "ABC1234",
            brand: "Fiat",
            model: "Uno"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new UpdateVehicleRequest(
            VehicleId: vehicleId,
            Brand: "VW",
            Model: "Gol",
            Color: "Prata",
            FuelType: (FuelType)1,
            FuelTankCapacityInLiters: 55m,
            Year: 2021,
            VehicleGroupId: groupId,
            PhotoBytes: null
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/vehicles/{vehicleId}", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        UpdateVehicleResponse? body = await ReadUpdateVehicleResponseAsync(response);

        Assert.IsNotNull(body);
        Assert.IsTrue(body!.UpdatedSuccessfully);
        Assert.AreEqual("VW", body.Brand);
        Assert.AreEqual("Gol", body.Model);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        Vehicle? vehicleFromDb = await dbContext.Vehicles
            .SingleOrDefaultAsync(v => v.Id == vehicleId);

        Assert.IsNotNull(vehicleFromDb);
        Assert.AreEqual("VW", vehicleFromDb!.Brand);
        Assert.AreEqual("Gol", vehicleFromDb.Model);
    }

    [TestMethod]
    public async Task DeleteVehicle_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid vehicleId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/vehicles/{vehicleId}");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteVehicle_Should_Return_BadRequest_When_VehicleId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-delete-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/vehicles/{emptyId}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors!.Count > 0);
    }

    [TestMethod]
    public async Task DeleteVehicle_Should_Return_NotFound_When_Vehicle_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-delete-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/vehicles/{nonExistingId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors!.Count > 0);
    }

    [TestMethod]
    public async Task DeleteVehicle_Should_Return_NotFound_When_Vehicle_Belongs_To_Other_Company()
    {
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-companyA-delete@test.com",
            password: "Senha123!"
        );

        Guid groupAId = await CreateVehicleGroupForCompanyAsync(
            companyAToken,
            rawName: "grupo empresa A para delete"
        );

        Guid vehicleId = await CreateVehicleForCompanyAsync(
            companyAToken,
            groupAId,
            licensePlate: "ABC1234",
            brand: "Fiat",
            model: "Uno"
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-companyB-delete@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/vehicles/{vehicleId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors!.Count > 0);
    }

    [TestMethod]
    public async Task DeleteVehicle_Should_Delete_Vehicle_When_Request_Is_Valid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-delete-ok@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid groupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            rawName: "grupo para exclusao de veiculo"
        );

        Guid vehicleId = await CreateVehicleForCompanyAsync(
            companyToken,
            groupId,
            licensePlate: "ABC1234",
            brand: "Fiat",
            model: "Uno"
        );

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        Vehicle? beforeDelete = await dbContext.Vehicles
            .SingleOrDefaultAsync(v => v.Id == vehicleId);

        Assert.IsNotNull(beforeDelete, "Veículo não localizado no banco antes do delete.");

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/vehicles/{vehicleId}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        DeleteVehicleResponse? deleteResponse =
            await response.Content.ReadFromJsonAsync<DeleteVehicleResponse>(JsonOptions);

        Assert.IsNotNull(deleteResponse);
        Assert.IsTrue(deleteResponse!.DeletedSuccessfully);
        Assert.AreEqual(vehicleId, deleteResponse.VehicleId);

        dbContext.ChangeTracker.Clear();

        Vehicle? afterDelete = await dbContext.Vehicles
            .SingleOrDefaultAsync(v => v.Id == vehicleId);

        Assert.IsNull(afterDelete, "Veículo ainda existe no banco após exclusão.");
    }

    [TestMethod]
    public async Task GetVehicleById_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid vehicleId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicles/{vehicleId}");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetVehicleById_Should_Return_BadRequest_When_VehicleId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-getbyid-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicles/{emptyId}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors!.Count > 0);
    }

    [TestMethod]
    public async Task GetVehicleById_Should_Return_NotFound_When_Vehicle_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-getbyid-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicles/{nonExistingId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors!.Count > 0);
    }

    [TestMethod]
    public async Task GetVehicleById_Should_Return_NotFound_When_Vehicle_Belongs_To_Other_Company()
    {
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-companyA-getbyid@test.com",
            password: "Senha123!"
        );

        Guid groupAId = await CreateVehicleGroupForCompanyAsync(
            companyAToken,
            rawName: "grupo empresa A"
        );

        Guid vehicleId = await CreateVehicleForCompanyAsync(
            companyAToken,
            groupAId,
            licensePlate: "ABC1234",
            brand: "Fiat",
            model: "Uno"
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-companyB-getbyid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicles/{vehicleId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors!.Count > 0);
    }

    [TestMethod]
    public async Task GetVehicleById_Should_Return_Vehicle_When_Request_Is_Valid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-getbyid-ok@test.com",
            password: "Senha123!"
        );

        Guid groupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            rawName: "grupo getbyid veiculo"
        );

        Guid vehicleId = await CreateVehicleForCompanyAsync(
            companyToken,
            groupId,
            licensePlate: "ABC1234",
            brand: "Fiat",
            model: "Uno"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicles/{vehicleId}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetVehicleByIdResponse? body =
            await ReadGetVehicleByIdResponseAsync(response);

        Assert.IsNotNull(body);
        Assert.AreEqual(vehicleId, body!.Id);
        Assert.AreEqual("ABC1234", body.LicensePlate);
        Assert.AreEqual("Fiat", body.Brand);
        Assert.AreEqual("Uno", body.Model);
        Assert.AreEqual(groupId, body.VehicleGroupId);
    }

    [TestMethod]
    public async Task GetAllVehicles_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/vehicles");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllVehicles_Should_Return_Empty_List_When_Company_Has_No_Vehicles()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-getall-empty@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/vehicles");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllVehiclesResponse? body =
            await ReadGetAllVehiclesResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.AreEqual(0, body!.Quantity);
        Assert.IsNotNull(body.Vehicles);
        Assert.AreEqual(0, body.Vehicles.Count);
    }

    [TestMethod]
    public async Task GetAllVehicles_Should_Return_All_Vehicles_When_Quantity_Is_Null()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-getall-ok@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid groupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            rawName: "grupo getall veiculos"
        );

        var plates = new[]
        {
            "AAA1234",
            "BBB1234",
            "CCC1234"
        };

        foreach (string plate in plates)
        {
            await CreateVehicleForCompanyAsync(
                companyToken,
                groupId,
                licensePlate: plate,
                brand: "Fiat",
                model: "Uno"
            );
        }

        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/vehicles");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllVehiclesResponse? body =
            await ReadGetAllVehiclesResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.AreEqual(plates.Length, body!.Quantity);
        Assert.AreEqual(plates.Length, body.Vehicles.Count);

        var actualPlates = body.Vehicles
            .Select(v => v.LicensePlate)
            .ToList();

        CollectionAssert.AreEquivalent(plates.ToList(), actualPlates);
    }

    [TestMethod]
    public async Task GetAllVehicles_Should_Return_BadRequest_When_Quantity_Is_Less_Than_Or_Equal_To_Zero()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-getall-qty-zero@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        int invalidQuantity = 0;

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicles?quantity={invalidQuantity}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors!.Count > 0);
    }

    [TestMethod]
    public async Task GetAllVehicles_Should_Filter_By_VehicleGroup_When_VehicleGroupId_Is_Informed()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "vehicles-getall-filter@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid group1Id = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            rawName: "grupo filtro 1"
        );

        Guid group2Id = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            rawName: "grupo filtro 2"
        );

        await CreateVehicleForCompanyAsync(
            companyToken,
            group1Id,
            licensePlate: "AAA1234",
            brand: "Fiat",
            model: "Uno"
        );

        await CreateVehicleForCompanyAsync(
            companyToken,
            group1Id,
            licensePlate: "BBB1234",
            brand: "VW",
            model: "Gol"
        );

        await CreateVehicleForCompanyAsync(
            companyToken,
            group2Id,
            licensePlate: "CCC1234",
            brand: "Chevrolet",
            model: "Onix"
        );

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/vehicles?vehicleGroupId={group1Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllVehiclesResponse? body =
            await ReadGetAllVehiclesResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.AreEqual(2, body!.Quantity);
        Assert.AreEqual(2, body.Vehicles.Count);
        Assert.IsTrue(body.Vehicles.All(v => v.VehicleGroupId == group1Id));
    }
}
