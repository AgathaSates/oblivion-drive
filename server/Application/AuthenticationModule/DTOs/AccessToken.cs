using OblivionDrive.Domain.AuthenticationModule;

namespace OblivionDrive.Application.AuthenticationModule.DTOs;
public record AccessToken(
    string key,
    DateTime expiration,
    AuthenticatedUser authenticatedUser
) : IAccessToken;