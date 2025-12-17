using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Api.Models.ClientModule.Requests;
using OblivionDrive.Api.Models.DriverModule.Requests;
using OblivionDrive.Api.Models.DriverModule.Responses;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Domain.VehicleModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.DriverModule;

[TestClass]
[TestCategory("Drivers - API Integration Tests")]
public class DriverIntegrationTests : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static DriverIntegrationTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static Task<AccessToken?> ReadAccessTokenAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<AccessToken>(JsonOptions);

    private static Task<List<string>?> ReadErrorsAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

    private static Task<RegisterDriverResponse?> ReadRegisterDriverResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<RegisterDriverResponse>(JsonOptions);

    private static Task<UpdateDriverResponse?> ReadUpdateDriverResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<UpdateDriverResponse>(JsonOptions);

    private static Task<DeleteDriverResponse?> ReadDeleteDriverResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<DeleteDriverResponse>(JsonOptions);

    private static Task<GetDriverByIdResponse?> ReadGetDriverByIdResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetDriverByIdResponse>(JsonOptions);

    private static Task<GetAllDriversResponse?> ReadGetAllDriversResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetAllDriversResponse>(JsonOptions);

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
            "Falha ao registrar usuário Company para o teste de condutores.");

        AccessToken? accessToken = await ReadAccessTokenAsync(response);
        Assert.IsNotNull(accessToken, "AccessToken não retornado pelo endpoint de registro.");

        return accessToken!;
    }

    private static RegisterClientRequest CreateValidClientRequest(string rawName, string email) =>
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

    private async Task<Guid> CreateClientForCompanyAsync(AccessToken companyToken, string rawName, string email)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        RegisterClientRequest request = CreateValidClientRequest(rawName, email);

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/clients", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Falha ao cadastrar o cliente usado no teste de condutores.");

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        Client? clientFromDb =
            await dbContext.Set<Client>()
                .SingleOrDefaultAsync(c => c.Email == email);

        Assert.IsNotNull(clientFromDb, $"Cliente com Email '{email}' não encontrado no banco após cadastro.");

        return clientFromDb!.Id;
    }

    private static RegisterDriverRequest CreateValidDriverRequest(
        string rawName,
        string email,
        Guid clientId,
        bool isClientAlsoDriver = false) =>
        new(
            Name: rawName,
            Email: email,
            PhoneNumber: "11988887777",
            Cpf: "98765432100",
            Cnh: "12345678901",
            CnhExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            ClientId: clientId,
            IsClientAlsoDriver: isClientAlsoDriver
        );

    private async Task<Guid> GetDriverIdByEmailAsync(string email)
    {
        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        var driverFromDb = await dbContext.Set<Driver>()
            .SingleOrDefaultAsync(d => d.Email == email);

        Assert.IsNotNull(driverFromDb, $"Condutor com Email '{email}' não encontrado no banco.");
        return driverFromDb!.Id;
    }

    private async Task<Guid> CreateDriverForCompanyAsync(AccessToken companyToken, RegisterDriverRequest request)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/drivers", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Falha ao cadastrar o condutor usado no teste.");

        return await GetDriverIdByEmailAsync(request.Email);
    }

    private async Task SeedOpenRentalForDriverAsync(AccessToken companyToken, Guid clientId, Guid driverId)
    {
        Guid companyId = companyToken.authenticatedUser.Id;

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        var vehicleGroup = new VehicleGroup(name: "grupo seed rental", companyId: companyId);

        var vehicle = new Vehicle(
            licensePlate: "AAA0A00",
            brand: "Marca",
            model: "Modelo",
            color: "Cor",
            fuelType: (FuelType)0,
            fuelTankCapacityInLiters: 50m,
            year: 2022,
            vehicleGroupId: vehicleGroup.Id,
            companyId: companyId
        );

        var rental = new Rental(
            companyId: companyId,
            clientId: clientId,
            driverId: driverId,
            vehicleId: vehicle.Id,
            planType: (RentalPlanType)0,
            startDate: DateOnly.FromDateTime(DateTime.Today),
            expectedReturnDate: DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            insuranceDailyPricePerPerson: 0m,
            insurancePersonsCount: 0,
            estimatedTotalKilometers: 100,
            servicesTotalPrice: 0m,
            insuranceTotalPrice: 0m,
            rentalBasePrice: 0m,
            estimatedRentalAmount: 0m,
            serviceIds: null
        );

        await dbContext.AddRangeAsync(vehicleGroup, vehicle, rental);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
    }

    [TestMethod]
    public async Task CreateDriver_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        var request = CreateValidDriverRequest(
            rawName: "condutor sem token",
            email: "driver.no-token@test.com",
            clientId: Guid.NewGuid()
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/drivers", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateDriver_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var invalidRequest = new RegisterDriverRequest(
            Name: string.Empty,
            Email: "invalid-email",
            PhoneNumber: string.Empty,
            Cpf: string.Empty,
            Cnh: string.Empty,
            CnhExpirationDate: default,
            ClientId: Guid.Empty,
            IsClientAlsoDriver: false
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/drivers", invalidRequest);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task CreateDriver_Should_Return_DriverResponse_When_Request_Is_Valid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-create-ok@test.com",
            password: "Senha123!"
        );

        Guid clientId = await CreateClientForCompanyAsync(
            companyToken,
            rawName: "cliente para condutor",
            email: "client.for-driver.create@test.com"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string rawName = "joao da silva";
        string email = "driver.create-ok@test.com";

        RegisterDriverRequest request = CreateValidDriverRequest(rawName, email, clientId);

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/drivers", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        RegisterDriverResponse? body = await ReadRegisterDriverResponseAsync(response);
        Assert.IsNotNull(body);
        Assert.IsTrue(body!.CreatedSuccessfully);

        string expectedName = NameFormatter.FormatName(rawName);

        Assert.AreEqual(expectedName, body.Name);
        Assert.AreEqual(email, body.Email);
        Assert.AreEqual(request.PhoneNumber, body.PhoneNumber);
        Assert.AreEqual(request.Cpf, body.Cpf);
        Assert.AreEqual(request.Cnh, body.Cnh);
        Assert.AreEqual(request.CnhExpirationDate, body.CnhExpirationDate);
        Assert.AreEqual(clientId, body.ClientId);
        Assert.AreEqual(request.IsClientAlsoDriver, body.IsClientAlsoDriver);

        Guid driverId = await GetDriverIdByEmailAsync(email);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        var driverFromDb = await dbContext.Set<Driver>()
            .SingleOrDefaultAsync(d => d.Id == driverId);

        Assert.IsNotNull(driverFromDb);
        Assert.AreEqual(expectedName, driverFromDb!.Name);
        Assert.AreEqual(email, driverFromDb.Email);
        Assert.AreEqual(clientId, driverFromDb.ClientId);
    }

    [TestMethod]
    public async Task UpdateDriver_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        Guid driverId = Guid.NewGuid();

        var request = new UpdateDriverRequest(
            Name: "Novo Nome",
            Email: "novo@email.com",
            PhoneNumber: "11977776666",
            Cpf: "98765432100",
            Cnh: "12345678901",
            CnhExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddYears(2)),
            ClientId: Guid.NewGuid(),
            IsClientAlsoDriver: false
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/drivers/{driverId}", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateDriver_Should_Return_BadRequest_When_DriverId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-update-emptyid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new UpdateDriverRequest(
            Name: "Nome Válido",
            Email: "driver.update.emptyid@test.com",
            PhoneNumber: "11977776666",
            Cpf: "98765432100",
            Cnh: "12345678901",
            CnhExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddYears(2)),
            ClientId: Guid.NewGuid(),
            IsClientAlsoDriver: false
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/drivers/{Guid.Empty}", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateDriver_Should_Return_NotFound_When_Driver_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-update-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingDriverId = Guid.NewGuid();

        var request = new UpdateDriverRequest(
            Name: "Condutor Inexistente",
            Email: "driver.update.notfound@test.com",
            PhoneNumber: "11977776666",
            Cpf: "98765432100",
            Cnh: "12345678901",
            CnhExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddYears(2)),
            ClientId: Guid.NewGuid(),
            IsClientAlsoDriver: false
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/drivers/{nonExistingDriverId}", request);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateDriver_Should_Return_NotFound_When_Driver_Belongs_To_Other_Company()
    {
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-companyA-update@test.com",
            password: "Senha123!"
        );

        Guid clientIdA = await CreateClientForCompanyAsync(
            companyAToken,
            rawName: "cliente A",
            email: "client.driver.companyA@test.com"
        );

        Guid driverIdA = await CreateDriverForCompanyAsync(
            companyAToken,
            CreateValidDriverRequest("condutor A", "driver.companyA@test.com", clientIdA)
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-companyB-update@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        var request = new UpdateDriverRequest(
            Name: "Tentativa Indevida",
            Email: "driver.companyB.try@test.com",
            PhoneNumber: "11977776666",
            Cpf: "11122233344",
            Cnh: "99999999999",
            CnhExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddYears(2)),
            ClientId: Guid.NewGuid(),
            IsClientAlsoDriver: false
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/drivers/{driverIdA}", request);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateDriver_Should_Return_Updated_Driver_When_Request_Is_Valid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-update-ok@test.com",
            password: "Senha123!"
        );

        Guid clientId = await CreateClientForCompanyAsync(
            companyToken,
            rawName: "cliente update driver",
            email: "client.driver.update@test.com"
        );

        Guid driverId = await CreateDriverForCompanyAsync(
            companyToken,
            CreateValidDriverRequest("condutor inicial", "driver.update-ok@test.com", clientId)
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string newRawName = "condutor atualizado";

        var request = new UpdateDriverRequest(
            Name: newRawName,
            Email: "driver.update-ok.new@test.com",
            PhoneNumber: "11911112222",
            Cpf: "22233344455",
            Cnh: "88888888888",
            CnhExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddYears(3)),
            ClientId: clientId,
            IsClientAlsoDriver: true
        );

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/drivers/{driverId}", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        UpdateDriverResponse? body = await ReadUpdateDriverResponseAsync(response);
        Assert.IsNotNull(body);
        Assert.IsTrue(body!.UpdatedSuccessfully);

        string expectedName = NameFormatter.FormatName(newRawName);

        Assert.AreEqual(expectedName, body.Name);
        Assert.AreEqual(request.Email, body.Email);
        Assert.AreEqual(request.PhoneNumber, body.PhoneNumber);
        Assert.AreEqual(request.Cpf, body.Cpf);
        Assert.AreEqual(request.Cnh, body.Cnh);
        Assert.AreEqual(request.CnhExpirationDate, body.CnhExpirationDate);
        Assert.AreEqual(clientId, body.ClientId);
        Assert.IsTrue(body.IsClientAlsoDriver);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        var driverFromDb = await dbContext.Set<Driver>()
            .SingleOrDefaultAsync(d => d.Id == driverId);

        Assert.IsNotNull(driverFromDb);
        Assert.AreEqual(expectedName, driverFromDb!.Name);
        Assert.AreEqual(request.Email, driverFromDb.Email);
        Assert.IsTrue(driverFromDb.IsClientAlsoDriver);
    }

    [TestMethod]
    public async Task DeleteDriver_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/drivers/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteDriver_Should_Return_BadRequest_When_DriverId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-delete-emptyid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/drivers/{Guid.Empty}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteDriver_Should_Return_NotFound_When_Driver_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-delete-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/drivers/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteDriver_Should_Return_NotFound_When_Driver_Belongs_To_Other_Company()
    {
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-companyA-delete@test.com",
            password: "Senha123!"
        );

        Guid clientIdA = await CreateClientForCompanyAsync(
            companyAToken,
            rawName: "cliente delete A",
            email: "client.driver.delete.companyA@test.com"
        );

        Guid driverIdA = await CreateDriverForCompanyAsync(
            companyAToken,
            CreateValidDriverRequest("condutor delete A", "driver.delete.companyA@test.com", clientIdA)
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-companyB-delete@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/drivers/{driverIdA}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteDriver_Should_Return_BadRequest_When_Driver_Has_Open_Rentals()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-delete-openrental@test.com",
            password: "Senha123!"
        );

        Guid clientId = await CreateClientForCompanyAsync(
            companyToken,
            rawName: "cliente com aluguel aberto",
            email: "client.driver.openrental@test.com"
        );

        Guid driverId = await CreateDriverForCompanyAsync(
            companyToken,
            CreateValidDriverRequest("condutor com aluguel", "driver.openrental@test.com", clientId)
        );

        await SeedOpenRentalForDriverAsync(companyToken, clientId, driverId);

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/drivers/{driverId}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteDriver_Should_Delete_Driver_When_Request_Is_Valid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-delete-ok@test.com",
            password: "Senha123!"
        );

        Guid clientId = await CreateClientForCompanyAsync(
            companyToken,
            rawName: "cliente para delete",
            email: "client.driver.delete@test.com"
        );

        string driverEmail = "driver.delete-ok@test.com";

        Guid driverId = await CreateDriverForCompanyAsync(
            companyToken,
            CreateValidDriverRequest("condutor para delete", driverEmail, clientId)
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        var beforeDelete = await dbContext.Set<Driver>()
            .SingleOrDefaultAsync(d => d.Id == driverId);

        Assert.IsNotNull(beforeDelete, "Condutor não localizado no banco antes do delete.");

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/drivers/{driverId}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        DeleteDriverResponse? body = await ReadDeleteDriverResponseAsync(response);
        Assert.IsNotNull(body);
        Assert.IsTrue(body!.DeletedSuccessfully);
        Assert.AreEqual(driverId, body.DriverId);

        dbContext.ChangeTracker.Clear();

        var afterDelete = await dbContext.Set<Driver>()
            .SingleOrDefaultAsync(d => d.Id == driverId);

        Assert.IsNull(afterDelete, "Condutor ainda existe no banco após exclusão.");
    }

    [TestMethod]
    public async Task GetDriverById_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/drivers/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetDriverById_Should_Return_BadRequest_When_DriverId_Is_Empty()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-getbyid-emptyid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/drivers/{Guid.Empty}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetDriverById_Should_Return_NotFound_When_Driver_Does_Not_Exist()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-getbyid-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/drivers/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetDriverById_Should_Return_NotFound_When_Driver_Belongs_To_Other_Company()
    {
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-companyA-getbyid@test.com",
            password: "Senha123!"
        );

        Guid clientIdA = await CreateClientForCompanyAsync(
            companyAToken,
            rawName: "cliente A getbyid",
            email: "client.driver.getbyid.companyA@test.com"
        );

        Guid driverIdA = await CreateDriverForCompanyAsync(
            companyAToken,
            CreateValidDriverRequest("condutor A getbyid", "driver.getbyid.companyA@test.com", clientIdA)
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-companyB-getbyid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/drivers/{driverIdA}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetDriverById_Should_Return_Driver_When_Request_Is_Valid()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-getbyid-ok@test.com",
            password: "Senha123!"
        );

        Guid clientId = await CreateClientForCompanyAsync(
            companyToken,
            rawName: "cliente getbyid driver",
            email: "client.driver.getbyid@test.com"
        );

        string rawName = "condutor get by id";
        string email = "driver.getbyid-ok@test.com";

        Guid driverId = await CreateDriverForCompanyAsync(
            companyToken,
            CreateValidDriverRequest(rawName, email, clientId, isClientAlsoDriver: true)
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/drivers/{driverId}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetDriverByIdResponse? body = await ReadGetDriverByIdResponseAsync(response);
        Assert.IsNotNull(body);

        string expectedName = NameFormatter.FormatName(rawName);

        Assert.AreEqual(driverId, body!.Id);
        Assert.AreEqual(expectedName, body.Name);
        Assert.AreEqual(email, body.Email);
        Assert.AreEqual(clientId, body.ClientId);
        Assert.IsTrue(body.IsClientAlsoDriver);
    }

    [TestMethod]
    public async Task GetAllDrivers_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/drivers");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllDrivers_Should_Return_Empty_List_When_Company_Has_No_Drivers()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-getall-empty@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/drivers");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllDriversResponse? body = await ReadGetAllDriversResponseAsync(response);

        Assert.IsNotNull(body);
        Assert.AreEqual(0, body!.Quantity);
        Assert.IsNotNull(body.Drivers);
        Assert.AreEqual(0, body.Drivers.Count);
    }

    [TestMethod]
    public async Task GetAllDrivers_Should_Return_BadRequest_When_Quantity_Is_Less_Than_Or_Equal_To_Zero()
    {
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "drivers-company-getall-qty-zero@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        int invalidQuantity = 0;

        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/drivers?quantity={invalidQuantity}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await ReadErrorsAsync(response);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }
}