namespace OblivionDrive.Api.Models.EmployeeModule;

public record UpdateOwnEmployeeResponse
{
    public bool UpdatedSuccessfully { get; init; }
    public string Name { get; init; } = string.Empty;
}