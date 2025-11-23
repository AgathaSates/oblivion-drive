namespace OblivionDrive.Api.Models.AuthenticationModule;

public record LoginUserRequest(
    string UserName,
    string Password);
