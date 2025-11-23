using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.AuthenticationModule;

[TestClass]
[TestCategory("Authentication - API Integration Tests")]
public class AuthenticationIntegrationTests : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static AuthenticationIntegrationTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static Task<AccessToken?> ReadAccessTokenAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<AccessToken>(JsonOptions);

    [TestMethod]
    public async Task Logout_Should_Return_Unauthorized_When_Token_Is_Invalid()
    {
        // arrange – token completamente inválido
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this-is-a-fake-invalid-token");

        // act
        var response = await HttpClient.PostAsync("/api/auth/logout", content: null);

        // assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Register_Should_Return_AccessToken_When_Request_Is_Valid()
    {
        // arrange
        var request = new RegisterUserRequest(
            UserName: "company@test.com",
            Email: "company@test.com",
            Password: "Senha123!"
        );

        // act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/register", request);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var accessToken = await ReadAccessTokenAsync(response);

        Assert.IsNotNull(accessToken);
        Assert.IsFalse(string.IsNullOrWhiteSpace(accessToken.key));
        Assert.IsNotNull(accessToken.authenticatedUser);
        Assert.AreEqual("company@test.com", accessToken.authenticatedUser.Email);
    }

    [TestMethod]
    public async Task Register_Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        var request = new RegisterUserRequest(
            UserName: "",
            Email: "invalid-email",
            Password: "123"
        );

        var response = await HttpClient.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var errors = await response.Content.ReadFromJsonAsync<List<string>>();

        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task Register_Should_Return_BadRequest_When_User_Already_Exists()
    {
        var request = new RegisterUserRequest(
            UserName: "duplicate@test.com",
            Email: "duplicate@test.com",
            Password: "Senha123!"
        );

        // primeiro cadastro
        var firstResponse = await HttpClient.PostAsJsonAsync("/api/auth/register", request);
        Assert.AreEqual(HttpStatusCode.OK, firstResponse.StatusCode);

        // segundo cadastro com mesmo login/email
        var secondResponse = await HttpClient.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var errors = await secondResponse.Content.ReadFromJsonAsync<List<string>>();
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Any());
    }

    [TestMethod]
    public async Task Login_Should_Return_AccessToken_When_Credentials_Are_Valid()
    {
        var userName = "loginuser@test.com";
        var password = "Senha123!";

        var registerRequest = new RegisterUserRequest(userName, userName, password);
        var registerResponse = await HttpClient.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginRequest = new LoginUserRequest(userName, password);
        var loginResponse = await HttpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);

        var accessToken = await ReadAccessTokenAsync(loginResponse);

        Assert.IsNotNull(accessToken);
        Assert.IsFalse(string.IsNullOrWhiteSpace(accessToken.key));
    }

    [TestMethod]
    public async Task Login_Should_Return_BadRequest_When_Validation_Fails()
    {
        var loginRequest = new LoginUserRequest(
            UserName: "ab",   
            Password: "123"
        );

        var loginResponse = await HttpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.AreEqual(HttpStatusCode.BadRequest, loginResponse.StatusCode);

        var errors = await loginResponse.Content.ReadFromJsonAsync<List<string>>();
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public async Task Login_Should_Return_BadRequest_When_User_Does_Not_Exist()
    {
        var loginRequest = new LoginUserRequest(
            UserName: "notfound@test.com",
            Password: "Senha123!"
        );

        var loginResponse = await HttpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.AreEqual(HttpStatusCode.BadRequest, loginResponse.StatusCode);
    }

    [TestMethod]
    public async Task Login_Should_Return_BadRequest_When_Password_Is_Incorrect()
    {
        var userName = "wrongpass@test.com";
        var password = "Senha123!";

        var registerRequest = new RegisterUserRequest(userName, userName, password);
        var registerResponse = await HttpClient.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginRequest = new LoginUserRequest(userName, "SenhaErrada123!");
        var loginResponse = await HttpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.AreEqual(HttpStatusCode.BadRequest, loginResponse.StatusCode);
    }

    [TestMethod]
    public async Task Logout_Should_Return_NoContent_When_Token_Is_Valid()
    {
        var userName = "logout@test.com";
        var password = "Senha123!";

        var registerRequest = new RegisterUserRequest(userName, userName, password);
        var registerResponse = await HttpClient.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        var accessToken = await ReadAccessTokenAsync(registerResponse);
        Assert.IsNotNull(accessToken);

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken.key);

        var logoutResponse = await HttpClient.PostAsync("/api/auth/logout", null);

        Assert.AreEqual(HttpStatusCode.NoContent, logoutResponse.StatusCode);
    }

    [TestMethod]
    public async Task Logout_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        var response = await HttpClient.PostAsync("/api/auth/logout", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}