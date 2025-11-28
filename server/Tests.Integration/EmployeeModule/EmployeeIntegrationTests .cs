using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Api.Models.EmployeeModule;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.EmployeeModule;
using OblivionDrive.Infrastructure.Orm.Shared;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.EmployeeModule;

[TestClass]
[TestCategory("Employee - API Integration Tests")]
public class EmployeeIntegrationTests : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static EmployeeIntegrationTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static Task<AccessToken?> ReadAccessTokenAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<AccessToken>(JsonOptions);

    private static Task<RegisterEmployeeResponse?> ReadRegisterEmployeeResponseAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<RegisterEmployeeResponse>(JsonOptions);

    private async Task<AccessToken> CreateEmployeeAndGetEmployeeTokenAsync()
    {
        const string companyUserName = "company-role-check@test.com";
        const string companyPassword = "Senha123!";
        const string employeeUserName = "employee-role-check@test.com";
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
            Name: "Joao da Silva",
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

    private async Task<AccessToken> RegisterCompanyAndGetTokenAsync(string userName, string password)
    {
        var request = new RegisterUserRequest(
            UserName: userName,
            Email: userName,
            Password: password
        );

        var response = await HttpClient.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Falha ao registrar usuário Company para o teste.");

        var accessToken = await ReadAccessTokenAsync(response);
        Assert.IsNotNull(accessToken, "AccessToken não retornado pelo endpoint de registro.");

        return accessToken!;
    }

    private async Task<Guid> GetEmployeeIdByUserNameAsync(string userName)
    {
        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado.");

        var employee = await dbContext.Employees
            .Include(e => e.IdentityUser)
            .SingleOrDefaultAsync(e => e.IdentityUser.UserName == userName);

        Assert.IsNotNull(employee, $"Funcionário com UserName '{userName}' não encontrado no banco para o teste.");

        return employee!.Id;
    }

    [TestMethod]
    public async Task RegisterEmployee_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        var request = new RegisterEmployeeRequest(
            UserName: "employee@test.com",
            Email: "employee@test.com",
            Password: "Senha123!",
            Name: "Joao da Silva",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3000m
        );

        // act
        var response = await HttpClient.PostAsJsonAsync("/api/employee/register", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task RegisterEmployee_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        // arrange
        var companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-employee-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new RegisterEmployeeRequest(
            UserName: string.Empty,
            Email: "invalid-email",
            Password: "123",
            Name: string.Empty,
            HireDate: new DateOnly(1960, 1, 1),
            Salary: 0m
        );

        // act
        var response = await HttpClient.PostAsJsonAsync("/api/employee/register", request);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);

        Assert.IsTrue(errors.Any(e => e.Contains("O nome de usuário é obrigatório")),
            "Mensagem de erro esperada para UserName não encontrada.");
        Assert.IsTrue(errors.Any(e => e.Contains("O e-mail deve estar no formato")),
            "Mensagem de erro esperada para Email não encontrada.");
    }

    [TestMethod]
    public async Task RegisterEmployee_Should_Return_Forbidden_When_User_Is_Employee()
    {
        // arrange
        AccessToken employeeToken = await CreateEmployeeAndGetEmployeeTokenAsync();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeToken.key);

        var request = new RegisterEmployeeRequest(
            UserName: "another.employee@test.com",
            Email: "another.employee@test.com",
            Password: "Senha123!",
            Name: "Funcionario Dois",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 2500m
        );

        // act
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("/api/employee/register", request);

        // assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task RegisterEmployee_Should_Return_EmployeeResponse_When_Request_Is_Valid()
    {
        // arrange
        var companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-employee-valid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var rawName = "joao da silva";
        var request = new RegisterEmployeeRequest(
            UserName: "employee.valid@test.com",
            Email: "employee.valid@test.com",
            Password: "Senha123!",
            Name: rawName,
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3000m
        );

        // act
        var response = await HttpClient.PostAsJsonAsync("/api/employee/register", request);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var employeeResponse = await ReadRegisterEmployeeResponseAsync(response);

        Assert.IsNotNull(employeeResponse);
        Assert.IsTrue(employeeResponse!.createdSuccessfully);

        var expectedName = NameFormatter.FormatName(rawName);
        Assert.AreEqual(expectedName, employeeResponse.Name);

        Assert.AreEqual(request.UserName, employeeResponse.UserName);
    }

    [TestMethod]
    public async Task UpdateEmployeeByCompany_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        Guid employeeId = Guid.NewGuid();

        var request = new UpdateEmployeeByCompanyRequest(
            Name: "Novo Nome",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3500m
        );

        var httpRequest = new HttpRequestMessage(
            new HttpMethod("PATCH"),
            $"/api/employee/{employeeId}")
        {
            Content = JsonContent.Create(request)
        };

        // act
        HttpResponseMessage response = await HttpClient.SendAsync(httpRequest);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateEmployeeByCompany_Should_Return_Forbidden_When_User_Is_Employee()
    {
        // arrange
        AccessToken employeeToken = await CreateEmployeeAndGetEmployeeTokenAsync();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeToken.key);

        Guid anyEmployeeId = Guid.NewGuid();

        var request = new UpdateEmployeeByCompanyRequest(
            Name: "Tentativa Indevida",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3000m
        );

        var httpRequest = new HttpRequestMessage(
            new HttpMethod("PATCH"),
            $"/api/employee/{anyEmployeeId}")
        {
            Content = JsonContent.Create(request)
        };

        // act
        HttpResponseMessage response = await HttpClient.SendAsync(httpRequest);

        // assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateEmployeeByCompany_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-update-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid invalidEmployeeId = Guid.Empty;

        var request = new UpdateEmployeeByCompanyRequest(
            Name: string.Empty,
            HireDate: new DateOnly(1960, 1, 1),
            Salary: 0m
        );

        var httpRequest = new HttpRequestMessage(
            new HttpMethod("PATCH"),
            $"/api/employee/{invalidEmployeeId}")
        {
            Content = JsonContent.Create(request)
        };

        // act
        HttpResponseMessage response = await HttpClient.SendAsync(httpRequest);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);

        Assert.IsTrue(errors.Any(e => e.Contains("O identificador do funcionário é obrigatório.")
                                      || e.Contains("O nome do funcionário é obrigatório.")));
    }

    [TestMethod]
    public async Task UpdateEmployeeByCompany_Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-update-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingEmployeeId = Guid.NewGuid();

        var request = new UpdateEmployeeByCompanyRequest(
            Name: "Funcionario Inexistente",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3000m
        );

        var httpRequest = new HttpRequestMessage(
            new HttpMethod("PATCH"),
            $"/api/employee/{nonExistingEmployeeId}")
        {
            Content = JsonContent.Create(request)
        };

        // act
        HttpResponseMessage response = await HttpClient.SendAsync(httpRequest);

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }
    
    [TestMethod]
    public async Task UpdateEmployeeByCompany_Should_Return_NotFound_When_Employee_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "companyA-update@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyAToken.key);

        string employeeUserName = "employee.other-company@test.com";

        var employeeRegisterRequest = new RegisterEmployeeRequest(
            UserName: employeeUserName,
            Email: employeeUserName,
            Password: "Senha123!",
            Name: "Funcionario Empresa A",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3000m
        );

        HttpResponseMessage registerResponse =
            await HttpClient.PostAsJsonAsync("/api/employee/register", employeeRegisterRequest);

        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        Guid employeeId = await GetEmployeeIdByUserNameAsync(employeeUserName);

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "companyB-update@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        var updateRequest = new UpdateEmployeeByCompanyRequest(
            Name: "Tentativa Indevida",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3500m
        );

        var httpRequest = new HttpRequestMessage(
            new HttpMethod("PATCH"),
            $"/api/employee/{employeeId}")
        {
            Content = JsonContent.Create(updateRequest)
        };

        // act
        HttpResponseMessage response = await HttpClient.SendAsync(httpRequest);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task UpdateEmployeeByCompany_Should_Return_Updated_Employee_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-update-ok@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string employeeUserName = "employee.update-ok@test.com";

        var initialRequest = new RegisterEmployeeRequest(
            UserName: employeeUserName,
            Email: employeeUserName,
            Password: "Senha123!",
            Name: "joao da silva",
            HireDate: new DateOnly(2020, 1, 1),
            Salary: 2000m
        );

        HttpResponseMessage registerResponse =
            await HttpClient.PostAsJsonAsync("/api/employee/register", initialRequest);

        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        Guid employeeId = await GetEmployeeIdByUserNameAsync(employeeUserName);

        string newRawName = "joao da silva atualizado";
        DateOnly newHireDate = new(2021, 2, 2);
        decimal newSalary = 4000m;

        var updateRequest = new UpdateEmployeeByCompanyRequest(
            Name: newRawName,
            HireDate: newHireDate,
            Salary: newSalary
        );

        var httpRequest = new HttpRequestMessage(
            new HttpMethod("PATCH"),
            $"/api/employee/{employeeId}")
        {
            Content = JsonContent.Create(updateRequest)
        };

        // act
        HttpResponseMessage response = await HttpClient.SendAsync(httpRequest);

        // assert 
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        UpdateEmployeeByCompanyResponse? updateResponse =
            await response.Content.ReadFromJsonAsync<UpdateEmployeeByCompanyResponse>(JsonOptions);

        Assert.IsNotNull(updateResponse);
        Assert.IsTrue(updateResponse!.UpdatedSuccessfully);

        string expectedFormattedName = NameFormatter.FormatName(newRawName);
        Assert.AreEqual(expectedFormattedName, updateResponse.Name);
        Assert.AreEqual(newHireDate, updateResponse.HireDate);
        Assert.AreEqual(newSalary, updateResponse.Salary);

        // assert
        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        Employee? employeeFromDb = await dbContext.Employees
            .Include(e => e.IdentityUser)
            .SingleOrDefaultAsync(e => e.Id == employeeId);

        Assert.IsNotNull(employeeFromDb);
        Assert.AreEqual(expectedFormattedName, employeeFromDb!.Name);
        Assert.AreEqual(newHireDate, employeeFromDb.HireDate);
        Assert.AreEqual(newSalary, employeeFromDb.Salary);
    }

    [TestMethod]
    public async Task UpdateOwnProfile_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        var request = new UpdateOwnEmployeeRequest("Novo Nome Válido");

        var httpRequest = new HttpRequestMessage(
            new HttpMethod("PATCH"),
            "/api/employee/profile")
        {
            Content = JsonContent.Create(request)
        };

        // act
        HttpResponseMessage response = await HttpClient.SendAsync(httpRequest);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateOwnProfile_Should_Return_Forbidden_When_User_Is_Company()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-update-profile@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        var request = new UpdateOwnEmployeeRequest("Nome Válido");

        var httpRequest = new HttpRequestMessage(
            new HttpMethod("PATCH"),
            "/api/employee/profile")
        {
            Content = JsonContent.Create(request)
        };

        // act
        HttpResponseMessage response = await HttpClient.SendAsync(httpRequest);

        // assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateOwnProfile_Should_Return_BadRequest_When_Name_Is_Invalid()
    {
        // arrange
        AccessToken employeeToken = await CreateEmployeeAndGetEmployeeTokenAsync();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeToken.key);

        var invalidRequest = new UpdateOwnEmployeeRequest(
            Name: string.Empty
        );

        var httpRequest = new HttpRequestMessage(
            new HttpMethod("PATCH"),
            "/api/employee/profile")
        {
            Content = JsonContent.Create(invalidRequest)
        };

        // act
        HttpResponseMessage response = await HttpClient.SendAsync(httpRequest);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);

        Assert.IsTrue(errors.Any(e =>
            e.Contains("O nome do funcionário é obrigatório.") ||
            e.Contains("deve ter pelo menos 2 caracteres") ||
            e.Contains("deve conter apenas letras e espaços")
        ));
    }

    [TestMethod]
    public async Task UpdateOwnProfile_Should_Update_Employee_Profile_When_Request_Is_Valid()
    {
        // arrange
        AccessToken employeeToken = await CreateEmployeeAndGetEmployeeTokenAsync();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeToken.key);

        string rawNewName = "joao da silva atualizado pelo proprio";
        var request = new UpdateOwnEmployeeRequest(rawNewName);

        var httpRequest = new HttpRequestMessage(
            new HttpMethod("PATCH"),
            "/api/employee/profile")
        {
            Content = JsonContent.Create(request)
        };

        // act
        HttpResponseMessage response = await HttpClient.SendAsync(httpRequest);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        UpdateOwnEmployeeResponse? updateResponse =
            await response.Content.ReadFromJsonAsync<UpdateOwnEmployeeResponse>(JsonOptions);

        Assert.IsNotNull(updateResponse);
        Assert.IsTrue(updateResponse!.UpdatedSuccessfully);

        string expectedFormattedName = NameFormatter.FormatName(rawNewName);
        Assert.AreEqual(expectedFormattedName, updateResponse.Name);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        Guid identityUserId = employeeToken.authenticatedUser.Id;

        Employee? employeeFromDb = await dbContext.Employees
            .Include(e => e.IdentityUser)
            .SingleOrDefaultAsync(e => e.IdentityUserId == identityUserId);

        Assert.IsNotNull(employeeFromDb);
        Assert.AreEqual(expectedFormattedName, employeeFromDb!.Name);
    }

    [TestMethod]
    public async Task DeleteEmployeeByCompany_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid employeeId = Guid.NewGuid();

        // act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"/api/employee/{employeeId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteEmployeeByCompany_Should_Return_Forbidden_When_User_Is_Employee()
    {
        // arrange
        AccessToken employeeToken = await CreateEmployeeAndGetEmployeeTokenAsync();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeToken.key);

        Guid anyEmployeeId = Guid.NewGuid();

        // act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"/api/employee/{anyEmployeeId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteEmployeeByCompany_Should_Return_BadRequest_When_EmployeeId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-delete-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        // act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"/api/employee/{emptyId}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e => e.Contains("O identificador do funcionário é obrigatório.")));
    }

    [TestMethod]
    public async Task DeleteEmployeeByCompany_Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-delete-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingEmployeeId = Guid.NewGuid();

        // act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"/api/employee/{nonExistingEmployeeId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteEmployeeByCompany_Should_Return_NotFound_When_Employee_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "companyA-delete@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyAToken.key);

        string employeeUserName = "employee.delete-other-company@test.com";

        var employeeRegisterRequest = new RegisterEmployeeRequest(
            UserName: employeeUserName,
            Email: employeeUserName,
            Password: "Senha123!",
            Name: "Funcionario Empresa A",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3000m
        );

        HttpResponseMessage registerResponse =
            await HttpClient.PostAsJsonAsync("/api/employee/register", employeeRegisterRequest);

        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        Guid employeeId = await GetEmployeeIdByUserNameAsync(employeeUserName);

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "companyB-delete@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        // act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"/api/employee/{employeeId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task DeleteEmployeeByCompany_Should_Delete_Employee_And_IdentityUser_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-delete-ok@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string employeeUserName = "employee.delete-ok@test.com";

        var registerRequest = new RegisterEmployeeRequest(
            UserName: employeeUserName,
            Email: employeeUserName,
            Password: "Senha123!",
            Name: "Funcionario Para Exclusao",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3000m
        );

        HttpResponseMessage registerResponse =
            await HttpClient.PostAsJsonAsync("/api/employee/register", registerRequest);

        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        Employee? employeeBeforeDelete = await dbContext.Employees
            .Include(e => e.IdentityUser)
            .SingleOrDefaultAsync(e => e.IdentityUser.UserName == employeeUserName);

        Assert.IsNotNull(employeeBeforeDelete);

        Guid employeeId = employeeBeforeDelete!.Id;
        Guid identityUserId = employeeBeforeDelete.IdentityUserId;

        // act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"/api/employee/{employeeId}");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        DeleteEmployeeByCompanyResponse? deleteResponse =
            await response.Content.ReadFromJsonAsync<DeleteEmployeeByCompanyResponse>(JsonOptions);

        Assert.IsNotNull(deleteResponse);
        Assert.IsTrue(deleteResponse!.DeletedSuccessfully);
        Assert.AreEqual(employeeId, deleteResponse.EmployeeId);

        dbContext.ChangeTracker.Clear();

        Employee? employeeAfterDelete = await dbContext.Employees
            .SingleOrDefaultAsync(e => e.Id == employeeId);

        var userAfterDelete = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == identityUserId);

        Assert.IsNull(employeeAfterDelete, "Funcionário ainda existe no banco após exclusão.");
        Assert.IsNull(userAfterDelete, "IdentityUser do funcionário ainda existe no banco após exclusão.");
    }

    [TestMethod]
    public async Task GetEmployeeByIdForCompany_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;
        Guid employeeId = Guid.NewGuid();

        // act
        HttpResponseMessage response = await HttpClient.GetAsync($"/api/employee/{employeeId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetEmployeeByIdForCompany_Should_Return_Forbidden_When_User_Is_Employee()
    {
        // arrange
        AccessToken employeeToken = await CreateEmployeeAndGetEmployeeTokenAsync();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeToken.key);

        Guid anyEmployeeId = Guid.NewGuid();

        // act
        HttpResponseMessage response = await HttpClient.GetAsync($"/api/employee/{anyEmployeeId}");

        // assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task GetEmployeeByIdForCompany_Should_Return_BadRequest_When_EmployeeId_Is_Empty()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-get-byid-invalid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid emptyId = Guid.Empty;

        // act
        HttpResponseMessage response = await HttpClient.GetAsync($"/api/employee/{emptyId}");

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any(e => e.Contains("O identificador do funcionário é obrigatório.")));
    }

    [TestMethod]
    public async Task GetEmployeeByIdForCompany_Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-get-byid-notfound@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        Guid nonExistingEmployeeId = Guid.NewGuid();

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/employee/{nonExistingEmployeeId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetEmployeeByIdForCompany_Should_Return_NotFound_When_Employee_Belongs_To_Other_Company()
    {
        // arrange
        AccessToken companyAToken = await RegisterCompanyAndGetTokenAsync(
            userName: "companyA-get-byid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyAToken.key);

        string employeeUserName = "employee.get-other-company@test.com";

        var employeeRegisterRequest = new RegisterEmployeeRequest(
            UserName: employeeUserName,
            Email: employeeUserName,
            Password: "Senha123!",
            Name: "Funcionario Empresa A",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3000m
        );

        HttpResponseMessage registerResponse =
            await HttpClient.PostAsJsonAsync("/api/employee/register", employeeRegisterRequest);

        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        Guid employeeId = await GetEmployeeIdByUserNameAsync(employeeUserName);

        AccessToken companyBToken = await RegisterCompanyAndGetTokenAsync(
            userName: "companyB-get-byid@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyBToken.key);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/employee/{employeeId}");

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        List<string>? errors =
            await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task GetEmployeeByIdForCompany_Should_Return_Employee_When_Request_Is_Valid()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-get-byid-ok@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string employeeUserName = "employee.get-byid-ok@test.com";
        string rawName = "joao da silva";
        DateOnly hireDate = new(2020, 1, 1);
        decimal salary = 2500m;

        var registerRequest = new RegisterEmployeeRequest(
            UserName: employeeUserName,
            Email: employeeUserName,
            Password: "Senha123!",
            Name: rawName,
            HireDate: hireDate,
            Salary: salary
        );

        HttpResponseMessage registerResponse =
            await HttpClient.PostAsJsonAsync("/api/employee/register", registerRequest);

        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        Guid employeeId = await GetEmployeeIdByUserNameAsync(employeeUserName);

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/employee/{employeeId}");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetEmployeeByCompanyResponse? employeeResponse =
            await response.Content.ReadFromJsonAsync<GetEmployeeByCompanyResponse>(JsonOptions);

        Assert.IsNotNull(employeeResponse);
        Assert.AreEqual(employeeId, employeeResponse!.Id);

        string expectedFormattedName = NameFormatter.FormatName(rawName);
        Assert.AreEqual(expectedFormattedName, employeeResponse.Name);
        Assert.AreEqual(hireDate, employeeResponse.HireDate);
        Assert.AreEqual(salary, employeeResponse.Salary);

        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext não inicializado no TestFixture.");

        dbContext.ChangeTracker.Clear();

        Employee? employeeFromDb = await dbContext.Employees
            .Include(e => e.IdentityUser)
            .SingleOrDefaultAsync(e => e.Id == employeeId);

        Assert.IsNotNull(employeeFromDb);
        Assert.AreEqual(expectedFormattedName, employeeFromDb!.Name);
        Assert.AreEqual(hireDate, employeeFromDb.HireDate);
        Assert.AreEqual(salary, employeeFromDb.Salary);
    }

    [TestMethod]
    public async Task GetAllEmployeesForCompany_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        // arrange
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // act
        HttpResponseMessage response = await HttpClient.GetAsync("/api/employee");

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllEmployeesForCompany_Should_Return_Forbidden_When_User_Is_Employee()
    {
        // arrange
        AccessToken employeeToken = await CreateEmployeeAndGetEmployeeTokenAsync();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeToken.key);

        // act
        HttpResponseMessage response = await HttpClient.GetAsync("/api/employee");

        // assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllEmployeesForCompany_Should_Return_Empty_List_When_Company_Has_No_Employees()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-no-employees@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        // act
        HttpResponseMessage response = await HttpClient.GetAsync("/api/employee");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllEmployeesForCompanyResponse? body =
            await response.Content.ReadFromJsonAsync<GetAllEmployeesForCompanyResponse>(JsonOptions);

        Assert.IsNotNull(body);
        Assert.AreEqual(0, body!.Quantity);
        Assert.IsNotNull(body.employees);
        Assert.AreEqual(0, body.employees.Count);
    }

    [TestMethod]
    public async Task GetAllEmployeesForCompany_Should_Return_All_Employees_When_Quantity_Is_Null()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-getall@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        string[] rawNames =
        [
            "funcionario um",
        "funcionario dois",
        "funcionario tres"
        ];

        for (int i = 0; i < rawNames.Length; i++)
        {
            string email = $"employee.getall.{i + 1}@test.com";

            var request = new RegisterEmployeeRequest(
                UserName: email,
                Email: email,
                Password: "Senha123!",
                Name: rawNames[i],
                HireDate: DateOnly.FromDateTime(DateTime.Today),
                Salary: 3000m + i
            );

            HttpResponseMessage registerResponse =
                await HttpClient.PostAsJsonAsync("/api/employee/register", request);

            Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);
        }

        // act
        HttpResponseMessage response = await HttpClient.GetAsync("/api/employee");

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        GetAllEmployeesForCompanyResponse? body =
            await response.Content.ReadFromJsonAsync<GetAllEmployeesForCompanyResponse>(JsonOptions);

        Assert.IsNotNull(body);
        Assert.AreEqual(rawNames.Length, body!.Quantity);
        Assert.AreEqual(rawNames.Length, body.employees.Count);

        var expectedNames = rawNames
            .Select(NameFormatter.FormatName)
            .ToList();

        var actualNames = body.employees
            .Select(e => e.Name)
            .ToList();

        CollectionAssert.AreEquivalent(expectedNames, actualNames);
    }

    [TestMethod]
    public async Task GetAllEmployeesForCompany_Should_Return_BadRequest_When_Quantity_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        AccessToken companyToken = await RegisterCompanyAndGetTokenAsync(
            userName: "company-getall-qty-zero@test.com",
            password: "Senha123!"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", companyToken.key);

        int invalidQuantity = 0;

        // act
        HttpResponseMessage response =
            await HttpClient.GetAsync($"/api/employee?quantity={invalidQuantity}");

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