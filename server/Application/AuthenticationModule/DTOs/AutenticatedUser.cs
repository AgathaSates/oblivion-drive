using OblivionDrive.Domain.AuthenticationModule;

namespace OblivionDrive.Application.AuthenticationModule.DTOs;
public record AuthenticatedUser(
    Guid Id,
    string Name,
    string Email,
    UserType UserType
);