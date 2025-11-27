namespace OblivionDrive.Application.EmployeeModule.DTOs;

public record EmployeeDTO
{
    public bool CreatedSuccessfully { get; init; }
    public string Name { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
}