namespace OblivionDrive.Application.EmployeeModule.DTOs;
public record DetailEmployeeDTO
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly HireDate { get; init; }
    public decimal Salary { get; init; }
}
