namespace OblivionDrive.Api.Models.AuthenticationModule;

public record RegisterUserRequest(
    string UserName,
    string Email,
    string Password);
