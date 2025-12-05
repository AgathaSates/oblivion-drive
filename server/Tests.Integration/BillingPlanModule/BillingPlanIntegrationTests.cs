using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Api.Models.BillingPlanModule;
using OblivionDrive.Api.Models.VehicleGroupModule;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.BillingPlanModule;

[TestClass]
[TestCategory("BillingPlans - API Integration Tests")]
public class BillingPlanIntegrationTests : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static BillingPlanIntegrationTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static Task<AccessToken?> ReadAccessTokenAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<AccessToken>(JsonOptions);

    private static Task<RegisterBillingPlanResponse?> ReadRegisterBillingPlanResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<RegisterBillingPlanResponse>(JsonOptions);

    private static Task<UpdateBillingPlanResponse?> ReadUpdateBillingPlanResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<UpdateBillingPlanResponse>(JsonOptions);

    private static Task<GetBillingPlanByIdResponse?> ReadGetBillingPlanByIdResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetBillingPlanByIdResponse>(JsonOptions);

    private static Task<GetAllBillingPlansResponse?> ReadGetAllBillingPlansResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<GetAllBillingPlansResponse>(JsonOptions);

    private static Task<DeleteBillingPlanResponse?> ReadDeleteBillingPlanResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<DeleteBillingPlanResponse>(JsonOptions);

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
            "Falha ao registrar usuário Company para o teste de planos de cobrança.");

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

    private async Task<Guid> CreateBillingPlanForCompanyAsync(
        AccessToken companyToken,
        Guid vehicleGroupId,
        string rawName,
        decimal dailyPlanDailyRate,
        decimal dailyPlanPricePerKilometer,
        decimal controlledPlanDailyRate,
        decimal controlledPlanExtraPricePerKilometer,
        decimal freePlanDailyRate)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new RegisterBillingPlanRequest(
            Name: rawName,
            VehicleGroupId: vehicleGroupId,
            DailyPlanDailyRate: dailyPlanDailyRate,
            DailyPlanPricePerKilometer: dailyPlanPricePerKilometer,
            ControlledPlanDailyRate: controlledPlanDailyRate,
            ControlledPlanExtraPricePerKilometer: controlledPlanExtraPricePerKilometer,
            FreePlanDailyRate: freePlanDailyRate
        );

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/billing-plans", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Falha ao cadastrar o plano de cobrança usado no teste.");

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        string expectedFormattedName = NameFormatter.FormatName(rawName);

        BillingPlan? billingPlanFromDb = await dbContext.BillingPlans
            .SingleOrDefaultAsync(bp => bp.Name == expectedFormattedName && bp.VehicleGroupId == vehicleGroupId);

        Assert.IsNotNull(billingPlanFromDb, "Plano de cobrança não encontrado no banco após cadastro.");

        return billingPlanFromDb!.Id;
    }

    [TestMethod]
    public async Task RegisterBillingPlan_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        var request = new RegisterBillingPlanRequest(
            Name: "plano sem token",
            VehicleGroupId: Guid.NewGuid(),
            DailyPlanDailyRate: 100m,
            DailyPlanPricePerKilometer: 1m,
            ControlledPlanDailyRate: 80m,
            ControlledPlanExtraPricePerKilometer: 2m,
            FreePlanDailyRate: 200m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/billing-plans", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task RegisterBillingPlan_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-company-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new RegisterBillingPlanRequest(
            Name: string.Empty,
            VehicleGroupId: Guid.Empty,
            DailyPlanDailyRate: 0m,
            DailyPlanPricePerKilometer: -1m,
            ControlledPlanDailyRate: 0m,
            ControlledPlanExtraPricePerKilometer: -1m,
            FreePlanDailyRate: 0m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/billing-plans", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);

        Assert.IsTrue(errors.Any(e => e.Contains("O nome do plano de cobrança é obrigatório.")),
            "Mensagem de erro esperada para Name não encontrada.");
        Assert.IsTrue(errors.Any(e => e.Contains("O identificador do grupo de veículos é obrigatório.")),
            "Mensagem de erro esperada para VehicleGroupId não encontrada.");
        Assert.IsTrue(errors.Any(e => e.Contains("A diária do plano diário deve ser maior que zero.")),
            "Mensagem de erro esperada para DailyPlanDailyRate não encontrada.");
    }

    [TestMethod]
    public async Task RegisterBillingPlan_Should_Return_BillingPlanResponse_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-company-valid@test.com",
            password: "Senha123!"
        );

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo para plano valido"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string rawName = "plano de cobrança teste";

        var request = new RegisterBillingPlanRequest(
            Name: rawName,
            VehicleGroupId: vehicleGroupId,
            DailyPlanDailyRate: 100m,
            DailyPlanPricePerKilometer: 1.5m,
            ControlledPlanDailyRate: 80m,
            ControlledPlanExtraPricePerKilometer: 2m,
            FreePlanDailyRate: 200m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/billing-plans", request);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        RegisterBillingPlanResponse? body =
            await ReadRegisterBillingPlanResponseAsync(response);

        Assert.IsNotNull(body);

        string expectedName = NameFormatter.FormatName(rawName);

        Assert.IsTrue(body!.CreatedSuccessfully);
        Assert.AreEqual(expectedName, body.Name);
        Assert.AreEqual(vehicleGroupId, body.VehicleGroupId);
        Assert.AreEqual(100m, body.DailyPlanDailyRate);
        Assert.AreEqual(1.5m, body.DailyPlanPricePerKilometer);
        Assert.AreEqual(80m, body.ControlledPlanDailyRate);
        Assert.AreEqual(2m, body.ControlledPlanExtraPricePerKilometer);
        Assert.AreEqual(200m, body.FreePlanDailyRate);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        BillingPlan? billingPlanFromDb = await dbContext.BillingPlans
            .SingleOrDefaultAsync(bp => bp.Name == expectedName && bp.VehicleGroupId == vehicleGroupId);

        Assert.IsNotNull(billingPlanFromDb);
        Assert.AreEqual(expectedName, billingPlanFromDb!.Name);
        Assert.AreEqual(vehicleGroupId, billingPlanFromDb.VehicleGroupId);
        Assert.AreEqual(100m, billingPlanFromDb.DailyPlan.DailyRate);
        Assert.AreEqual(1.5m, billingPlanFromDb.DailyPlan.PricePerKilometer);
        Assert.AreEqual(80m, billingPlanFromDb.ControlledPlan.DailyRate);
        Assert.AreEqual(2m, billingPlanFromDb.ControlledPlan.ExtraPricePerKilometer);
        Assert.AreEqual(200m, billingPlanFromDb.FreePlan.DailyRate);
    }

    [TestMethod]
    public async Task RegisterBillingPlan_Should_Return_BadRequest_When_Name_Already_Exists_For_Company()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-duplicated-name@test.com",
            password: "Senha123!"
        );

        Guid vehicleGroupId1 = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo plano nome duplicado 1"
        );

        Guid vehicleGroupId2 = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo plano nome duplicado 2"
        );

        string rawName = "plano duplicado";

        await CreateBillingPlanForCompanyAsync(
            companyToken,
            vehicleGroupId1,
            rawName,
            100m,
            1.5m,
            80m,
            2m,
            200m
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new RegisterBillingPlanRequest(
            Name: rawName,
            VehicleGroupId: vehicleGroupId2,
            DailyPlanDailyRate: 120m,
            DailyPlanPricePerKilometer: 1.2m,
            ControlledPlanDailyRate: 90m,
            ControlledPlanExtraPricePerKilometer: 1.8m,
            FreePlanDailyRate: 220m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/billing-plans", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e =>
            e.Contains("Já existe um plano de cobrança cadastrado com este nome para esta empresa.")));
    }

    [TestMethod]
    public async Task RegisterBillingPlan_Should_Return_BadRequest_When_BillingPlan_For_VehicleGroup_Already_Exists()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-duplicated-group@test.com",
            password: "Senha123!"
        );

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo com plano existente"
        );

        await CreateBillingPlanForCompanyAsync(
            companyToken,
            vehicleGroupId,
            "plano existente",
            100m,
            1.5m,
            80m,
            2m,
            200m
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new RegisterBillingPlanRequest(
            Name: "outro plano",
            VehicleGroupId: vehicleGroupId,
            DailyPlanDailyRate: 150m,
            DailyPlanPricePerKilometer: 2m,
            ControlledPlanDailyRate: 100m,
            ControlledPlanExtraPricePerKilometer: 3m,
            FreePlanDailyRate: 250m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/billing-plans", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e =>
            e.Contains("Já existe um plano de cobrança cadastrado para este grupo de veículos.")));
    }

    [TestMethod]
    public async Task UpdateBillingPlan_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid billingPlanId = Guid.NewGuid();

        var request = new UpdateBillingPlanRequest(
            Name: "plano atualizado",
            VehicleGroupId: Guid.NewGuid(),
            DailyPlanDailyRate: 150m,
            DailyPlanPricePerKilometer: 2m,
            ControlledPlanDailyRate: 100m,
            ControlledPlanExtraPricePerKilometer: 3m,
            FreePlanDailyRate: 250m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/billing-plans/{billingPlanId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateBillingPlan_Should_Return_BadRequest_When_BillingPlanId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-update-invalid-id@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        var request = new UpdateBillingPlanRequest(
            Name: string.Empty,
            VehicleGroupId: Guid.Empty,
            DailyPlanDailyRate: 0m,
            DailyPlanPricePerKilometer: -1m,
            ControlledPlanDailyRate: 0m,
            ControlledPlanExtraPricePerKilometer: -1m,
            FreePlanDailyRate: 0m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/billing-plans/{emptyId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e =>
            e.Contains("O identificador do plano de cobrança é obrigatório.")));
    }

    [TestMethod]
    public async Task UpdateBillingPlan_Should_Return_NotFound_When_BillingPlan_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-update-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        var request = new UpdateBillingPlanRequest(
            Name: "plano inexistente",
            VehicleGroupId: Guid.NewGuid(),
            DailyPlanDailyRate: 150m,
            DailyPlanPricePerKilometer: 2m,
            ControlledPlanDailyRate: 100m,
            ControlledPlanExtraPricePerKilometer: 3m,
            FreePlanDailyRate: 250m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/billing-plans/{nonExistingId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateBillingPlan_Should_Return_NotFound_When_BillingPlan_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-companyA-update@test.com",
            password: "Senha123!"
        );

        Guid vehicleGroupIdA = await CreateVehicleGroupForCompanyAsync(
            companyAToken,
            "grupo empresa A"
        );

        Guid billingPlanId = await CreateBillingPlanForCompanyAsync(
            companyAToken,
            vehicleGroupIdA,
            "plano empresa A",
            100m,
            1.5m,
            80m,
            2m,
            200m
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-companyB-update@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        var request = new UpdateBillingPlanRequest(
            Name: "tentativa indevida",
            VehicleGroupId: vehicleGroupIdA,
            DailyPlanDailyRate: 150m,
            DailyPlanPricePerKilometer: 2m,
            ControlledPlanDailyRate: 100m,
            ControlledPlanExtraPricePerKilometer: 3m,
            FreePlanDailyRate: 250m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/billing-plans/{billingPlanId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateBillingPlan_Should_Return_Updated_BillingPlan_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-update-ok@test.com",
            password: "Senha123!"
        );

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo inicial"
        );

        Guid billingPlanId = await CreateBillingPlanForCompanyAsync(
            companyToken,
            vehicleGroupId,
            "plano inicial",
            100m,
            1.5m,
            80m,
            2m,
            200m
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string newRawName = "plano atualizado";

        var request = new UpdateBillingPlanRequest(
            Name: newRawName,
            VehicleGroupId: vehicleGroupId,
            DailyPlanDailyRate: 150m,
            DailyPlanPricePerKilometer: 2m,
            ControlledPlanDailyRate: 100m,
            ControlledPlanExtraPricePerKilometer: 3m,
            FreePlanDailyRate: 250m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/billing-plans/{billingPlanId}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        UpdateBillingPlanResponse? body =
            await ReadUpdateBillingPlanResponseAsync(response);

        Assert.IsNotNull(body);

        string expectedFormattedName = NameFormatter.FormatName(newRawName);

        Assert.IsTrue(body!.UpdatedSuccessfully);
        Assert.AreEqual(expectedFormattedName, body.Name);
        Assert.AreEqual(vehicleGroupId, body.VehicleGroupId);
        Assert.AreEqual(150m, body.DailyPlanDailyRate);
        Assert.AreEqual(2m, body.DailyPlanPricePerKilometer);
        Assert.AreEqual(100m, body.ControlledPlanDailyRate);
        Assert.AreEqual(3m, body.ControlledPlanExtraPricePerKilometer);
        Assert.AreEqual(250m, body.FreePlanDailyRate);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        BillingPlan? billingPlanFromDb = await dbContext.BillingPlans
            .SingleOrDefaultAsync(bp => bp.Id == billingPlanId);

        Assert.IsNotNull(billingPlanFromDb);

        Assert.AreEqual(expectedFormattedName, billingPlanFromDb!.Name);
        Assert.AreEqual(vehicleGroupId, billingPlanFromDb.VehicleGroupId);
        Assert.AreEqual(150m, billingPlanFromDb.DailyPlan.DailyRate);
        Assert.AreEqual(2m, billingPlanFromDb.DailyPlan.PricePerKilometer);
        Assert.AreEqual(100m, billingPlanFromDb.ControlledPlan.DailyRate);
        Assert.AreEqual(3m, billingPlanFromDb.ControlledPlan.ExtraPricePerKilometer);
        Assert.AreEqual(250m, billingPlanFromDb.FreePlan.DailyRate);
    }

    [TestMethod]
    public async Task UpdateBillingPlan_Should_Return_BadRequest_When_Name_Already_Exists_For_Other_BillingPlan()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-update-duplicated-name@test.com",
            password: "Senha123!"
        );

        Guid group1Id = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo 1"
        );

        Guid group2Id = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo 2"
        );

        string existingName = "plano existente";

        await CreateBillingPlanForCompanyAsync(
            companyToken,
            group1Id,
            existingName,
            100m,
            1.5m,
            80m,
            2m,
            200m
        );

        Guid billingPlanIdToUpdate = await CreateBillingPlanForCompanyAsync(
            companyToken,
            group2Id,
            "outro plano",
            120m,
            1.2m,
            90m,
            1.8m,
            220m
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new UpdateBillingPlanRequest(
            Name: existingName,
            VehicleGroupId: group2Id,
            DailyPlanDailyRate: 130m,
            DailyPlanPricePerKilometer: 1.3m,
            ControlledPlanDailyRate: 95m,
            ControlledPlanExtraPricePerKilometer: 1.9m,
            FreePlanDailyRate: 230m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/billing-plans/{billingPlanIdToUpdate}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e =>
            e.Contains("Já existe um plano de cobrança cadastrado com este nome para esta empresa.")));
    }

    [TestMethod]
    public async Task UpdateBillingPlan_Should_Return_BadRequest_When_VehicleGroup_Already_Has_Other_BillingPlan()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-update-duplicated-group@test.com",
            password: "Senha123!"
        );

        Guid group1Id = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo 1 para update"
        );

        Guid group2Id = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo 2 para update"
        );

        Guid billingPlanIdGroup1 = await CreateBillingPlanForCompanyAsync(
            companyToken,
            group1Id,
            "plano grupo 1",
            100m,
            1.5m,
            80m,
            2m,
            200m
        );

        Guid billingPlanIdGroup2 = await CreateBillingPlanForCompanyAsync(
            companyToken,
            group2Id,
            "plano grupo 2",
            120m,
            1.2m,
            90m,
            1.8m,
            220m
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new UpdateBillingPlanRequest(
            Name: "novo nome sem conflito",
            VehicleGroupId: group1Id,
            DailyPlanDailyRate: 130m,
            DailyPlanPricePerKilometer: 1.3m,
            ControlledPlanDailyRate: 95m,
            ControlledPlanExtraPricePerKilometer: 1.9m,
            FreePlanDailyRate: 230m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"/api/billing-plans/{billingPlanIdGroup2}", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e =>
            e.Contains("Já existe outro plano de cobrança cadastrado para este grupo de veículos.")));
    }

    [TestMethod]
    public async Task DeleteBillingPlan_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid billingPlanId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/billing-plans/{billingPlanId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteBillingPlan_Should_Return_BadRequest_When_BillingPlanId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-delete-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/billing-plans/{emptyId}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e =>
            e.Contains("O identificador do plano de cobrança é obrigatório.")));
    }

    [TestMethod]
    public async Task DeleteBillingPlan_Should_Return_NotFound_When_BillingPlan_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-delete-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/billing-plans/{nonExistingId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteBillingPlan_Should_Return_NotFound_When_BillingPlan_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-companyA-delete@test.com",
            password: "Senha123!"
        );

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyAToken,
            "grupo empresa A delete"
        );

        Guid billingPlanId = await CreateBillingPlanForCompanyAsync(
            companyAToken,
            vehicleGroupId,
            "plano empresa A delete",
            100m,
            1.5m,
            80m,
            2m,
            200m
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-companyB-delete@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/billing-plans/{billingPlanId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteBillingPlan_Should_Delete_BillingPlan_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-delete-ok@test.com",
            password: "Senha123!"
        );

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo para exclusao plano"
        );

        Guid billingPlanId = await CreateBillingPlanForCompanyAsync(
            companyToken,
            vehicleGroupId,
            "plano para exclusao",
            100m,
            1.5m,
            80m,
            2m,
            200m
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        BillingPlan? beforeDelete = await dbContext.BillingPlans
            .SingleOrDefaultAsync(bp => bp.Id == billingPlanId);

        Assert.IsNotNull(beforeDelete, "Plano de cobrança não localizado no banco antes do delete.");

        // act
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"/api/billing-plans/{billingPlanId}");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        DeleteBillingPlanResponse? deleteResponse =
            await ReadDeleteBillingPlanResponseAsync(response);

        Assert.IsNotNull(deleteResponse);
        Assert.IsTrue(deleteResponse!.DeletedSuccessfully);
        Assert.AreEqual(billingPlanId, deleteResponse.BillingPlanId);

        dbContext.ChangeTracker.Clear();

        BillingPlan? afterDelete = await dbContext.BillingPlans
            .SingleOrDefaultAsync(bp => bp.Id == billingPlanId);

        Assert.IsNull(afterDelete, "Plano de cobrança ainda existe no banco após exclusão.");
    }

    [TestMethod]
    public async Task GetBillingPlanById_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid billingPlanId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/billing-plans/{billingPlanId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetBillingPlanById_Should_Return_BadRequest_When_BillingPlanId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-getbyid-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/billing-plans/{emptyId}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e =>
            e.Contains("O identificador do plano de cobrança é obrigatório.")));
    }

    [TestMethod]
    public async Task GetBillingPlanById_Should_Return_NotFound_When_BillingPlan_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-getbyid-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/billing-plans/{nonExistingId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetBillingPlanById_Should_Return_NotFound_When_BillingPlan_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-companyA-getbyid@test.com",
            password: "Senha123!"
        );

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyAToken,
            "grupo empresa A getbyid"
        );

        Guid billingPlanId = await CreateBillingPlanForCompanyAsync(
            companyAToken,
            vehicleGroupId,
            "plano empresa A getbyid",
            100m,
            1.5m,
            80m,
            2m,
            200m
        );

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-companyB-getbyid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/billing-plans/{billingPlanId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetBillingPlanById_Should_Return_BillingPlan_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-getbyid-ok@test.com",
            password: "Senha123!"
        );

        Guid vehicleGroupId = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo getbyid"
        );

        string rawName = "plano get by id ok";

        Guid billingPlanId = await CreateBillingPlanForCompanyAsync(
            companyToken,
            vehicleGroupId,
            rawName,
            100m,
            1.5m,
            80m,
            2m,
            200m
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/billing-plans/{billingPlanId}");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetBillingPlanByIdResponse? body =
            await ReadGetBillingPlanByIdResponseAsync(response);

        Assert.IsNotNull(body);

        string expectedName = NameFormatter.FormatName(rawName);

        Assert.AreEqual(billingPlanId, body!.Id);
        Assert.AreEqual(expectedName, body.Name);
        Assert.AreEqual(vehicleGroupId, body.VehicleGroupId);
        Assert.AreEqual(100m, body.DailyPlanDailyRate);
        Assert.AreEqual(1.5m, body.DailyPlanPricePerKilometer);
        Assert.AreEqual(80m, body.ControlledPlanDailyRate);
        Assert.AreEqual(2m, body.ControlledPlanExtraPricePerKilometer);
        Assert.AreEqual(200m, body.FreePlanDailyRate);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");
        dbContext.ChangeTracker.Clear();

        BillingPlan? billingPlanFromDb = await dbContext.BillingPlans
            .SingleOrDefaultAsync(bp => bp.Id == billingPlanId);

        Assert.IsNotNull(billingPlanFromDb);

        Assert.AreEqual(expectedName, billingPlanFromDb!.Name);
        Assert.AreEqual(vehicleGroupId, billingPlanFromDb.VehicleGroupId);
        Assert.AreEqual(100m, billingPlanFromDb.DailyPlan.DailyRate);
        Assert.AreEqual(1.5m, billingPlanFromDb.DailyPlan.PricePerKilometer);
        Assert.AreEqual(80m, billingPlanFromDb.ControlledPlan.DailyRate);
        Assert.AreEqual(2m, billingPlanFromDb.ControlledPlan.ExtraPricePerKilometer);
        Assert.AreEqual(200m, billingPlanFromDb.FreePlan.DailyRate);
    }

    [TestMethod]
    public async Task GetAllBillingPlans_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/billing-plans");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllBillingPlans_Should_Return_Empty_List_When_Company_Has_No_BillingPlans()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-getall-empty@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/billing-plans");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllBillingPlansResponse? body =
            await ReadGetAllBillingPlansResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.AreEqual(0, body!.Quantity);
        Assert.IsNotNull(body.BillingPlans);
        Assert.AreEqual(0, body.BillingPlans.Count);
    }

    [TestMethod]
    public async Task GetAllBillingPlans_Should_Return_All_BillingPlans_When_Quantity_Is_Null()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-getall-ok@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid vehicleGroupId1 = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo all 1"
        );

        Guid vehicleGroupId2 = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo all 2"
        );

        Guid vehicleGroupId3 = await CreateVehicleGroupForCompanyAsync(
            companyToken,
            "grupo all 3"
        );

        string[] rawNames =
        [
            "plano um",
            "plano dois",
            "plano tres"
        ];

        Guid[] groupIds = [vehicleGroupId1, vehicleGroupId2, vehicleGroupId3];

        for (int i = 0; i < rawNames.Length; i++)
        {
            string rawName = rawNames[i];
            Guid vehicleGroupId = groupIds[i];

            var request = new RegisterBillingPlanRequest(
                Name: rawName,
                VehicleGroupId: vehicleGroupId,
                DailyPlanDailyRate: 100m + i,
                DailyPlanPricePerKilometer: 1m + i,
                ControlledPlanDailyRate: 80m + i,
                ControlledPlanExtraPricePerKilometer: 2m + i,
                FreePlanDailyRate: 200m + i
            );

            HttpResponseMessage registerResponse =
                await HttpClient.PostAsJsonAsync("/api/billing-plans", request);

            Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);
        }

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/billing-plans");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllBillingPlansResponse? body =
            await ReadGetAllBillingPlansResponseAsync(response);

        Assert.IsNotNull(body);

        Assert.AreEqual(rawNames.Length, body!.Quantity);
        Assert.AreEqual(rawNames.Length, body.BillingPlans.Count);

        var expectedNames = rawNames
            .Select(NameFormatter.FormatName)
            .ToList();

        var actualNames = body.BillingPlans
            .Select(bp => bp.Name)
            .ToList();

        CollectionAssert.AreEquivalent(expectedNames, actualNames);
    }

    [TestMethod]
    public async Task GetAllBillingPlans_Should_Return_BadRequest_When_Quantity_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "billingplans-getall-qty-zero@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        int invalidQuantity = 0;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/billing-plans?quantity={invalidQuantity}");

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