using OblivionDrive.Domain.AuthenticationModule;

namespace OblivionDrive.Application.AuthenticationModule.DTOs;
public record AuthenticatedUser(
    Guid Id,
    string UserName,
    string Email,
    UserType UserType
);