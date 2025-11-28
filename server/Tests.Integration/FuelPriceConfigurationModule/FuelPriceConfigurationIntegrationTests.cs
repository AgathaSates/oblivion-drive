using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Api.Models.EmployeeModule;
using OblivionDrive.Api.Models.FuelPriceConfigurationModule;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Application.FuelPriceConfigurationModule.DTOs;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.FuelPriceConfigurationModule;

[TestClass]
[TestCategory("FuelPriceConfiguration - API Integration Tests")]
public sealed class FuelPriceConfigurationIntegrationTests : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static FuelPriceConfigurationIntegrationTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static Task<AccessToken?> ReadAccessTokenAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<AccessToken>(JsonOptions);

    private static Task<FuelPriceConfigurationDto?> ReadFuelPriceConfigurationAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<FuelPriceConfigurationDto>(JsonOptions);

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
            "Falha ao registrar usuário Company para o teste.");

        AccessToken? accessToken = await ReadAccessTokenAsync(response);
        Assert.IsNotNull(accessToken, "AccessToken não retornado pelo endpoint de registro.");

        return accessToken!;
    }

    private async Task<AccessToken> CreateEmployeeAndGetEmployeeTokenAsync()
    {
        const string companyUserName = "fuel-company-role-check@test.com";
        const string companyPassword = "Senha123!";
        const string employeeUserName = "fuel-employee-role-check@test.com";
        const string employeePassword = "Senha123!";

        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: companyUserName,
            password: companyPassword
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var employeeRegisterRequest = new RegisterEmployeeRequest(
            UserName: employeeUserName,
            Email: employeeUserName,
            Password: employeePassword,
            Name: "Funcionario Fuel",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3000m
        );

        HttpResponseMessage employeeRegisterResponse =
            await HttpClient.PostAsJsonAsync("/api/employee/register", employeeRegisterRequest);

        Assert.AreEqual(HttpStatusCode.OK, employeeRegisterResponse.StatusCode,
            "Falha ao registrar o funcionário usado no teste de role Employee.");

        HttpClient.DefaultRequestHeaders.Authorization = null;

        var loginRequest = new LoginUserRequest(
            UserName: employeeUserName,
            Password: employeePassword
        );

        HttpResponseMessage loginResponse =
            await HttpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode,
            "Falha ao fazer login com o usuário Employee.");

        AccessToken? employeeToken = await ReadAccessTokenAsync(loginResponse);
        Assert.IsNotNull(employeeToken, "AccessToken do Employee não foi retornado.");

        return employeeToken!;
    }

    [TestMethod]
    public async Task GetFuelPriceConfiguration_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/fuel-price-configuration");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetFuelPriceConfiguration_Should_Return_Configuration_When_User_Is_Company()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "fuel-company-get@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        // act
        var beforeMinDate = DateOnly.FromDateTime(DateTime.Now);
        HttpResponseMessage response =
            await HttpClient.GetAsync("/api/fuel-price-configuration");
        var afterMaxDate = DateOnly.FromDateTime(DateTime.Now);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        FuelPriceConfigurationDto? body =
            await ReadFuelPriceConfigurationAsync(response);

        Assert.IsNotNull(body);

        Assert.AreEqual(0m, body!.Gasoline);
        Assert.AreEqual(0m, body.Gas);
        Assert.AreEqual(0m, body.Diesel);
        Assert.AreEqual(0m, body.Alcohol);

        Assert.IsTrue(
            body.LastUpdate >= beforeMinDate &&
            body.LastUpdate <= afterMaxDate,
            "LastUpdate deve ser a data atual (ou muito próxima) no primeiro GET.");
    }

    [TestMethod]
    public async Task UpdateFuelPriceConfiguration_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        var request = new UpdateFuelPriceConfigurationRequest(5.79m, 4.10m, 6.20m,3.99m);

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync("/api/fuel-price-configuration", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateFuelPriceConfiguration_Should_Return_Forbidden_When_User_Is_Employee()
    {
        // arrange
        AccessToken employeeToken = await CreateEmployeeAndGetEmployeeTokenAsync();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeToken.key);

        var request = new UpdateFuelPriceConfigurationRequest(5.79m, 4.10m, 6.20m, 3.99m);

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync("/api/fuel-price-configuration", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateFuelPriceConfiguration_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "fuel-company-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new UpdateFuelPriceConfigurationRequest(0m, 4.10m, 6.20m, 3.99m);

        // act
        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync("/api/fuel-price-configuration", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);

        Assert.IsTrue(
            errors.Any(e => e.Contains("O preço da gasolina deve ser maior que zero")),
            "Mensagem de validação esperada para preço de gasolina inválido não encontrada.");
    }

    [TestMethod]
    public async Task UpdateFuelPriceConfiguration_Should_Update_Configuration_And_Return_Success()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "fuel-company-update-ok@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        HttpResponseMessage initialGetResponse =
            await HttpClient.GetAsync("/api/fuel-price-configuration");

        Assert.AreEqual(HttpStatusCode.OK, initialGetResponse.StatusCode);

        var updateRequest = new UpdateFuelPriceConfigurationRequest(5.79m, 4.10m, 6.20m, 3.99m);

        // act
        HttpResponseMessage updateResponse =
            await HttpClient.PutAsJsonAsync("/api/fuel-price-configuration", updateRequest);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);

        FuelPriceConfigurationDto? updatedBody =
            await ReadFuelPriceConfigurationAsync(updateResponse);

        Assert.IsNotNull(updatedBody);
        Assert.AreEqual(updateRequest.Gasoline, updatedBody!.Gasoline);
        Assert.AreEqual(updateRequest.Gas, updatedBody.Gas);
        Assert.AreEqual(updateRequest.Diesel, updatedBody.Diesel);
        Assert.AreEqual(updateRequest.Alcohol, updatedBody.Alcohol);

        HttpResponseMessage finalGetResponse =
            await HttpClient.GetAsync("/api/fuel-price-configuration");

        Assert.AreEqual(HttpStatusCode.OK, finalGetResponse.StatusCode);

        FuelPriceConfigurationDto? finalBody =
            await ReadFuelPriceConfigurationAsync(finalGetResponse);

        Assert.IsNotNull(finalBody);
        Assert.AreEqual(updateRequest.Gasoline, finalBody!.Gasoline);
        Assert.AreEqual(updateRequest.Gas, finalBody.Gas);
        Assert.AreEqual(updateRequest.Diesel, finalBody.Diesel);
        Assert.AreEqual(updateRequest.Alcohol, finalBody.Alcohol);
    }
}