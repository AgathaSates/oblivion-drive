namespace OblivionDrive.Application.EmployeeModule.DTOs;
public record UpdatedEmployeeDTO
{
    public bool UpdatedSuccessfully { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly HireDate { get; init; }
    public decimal Salary { get; init; }
}