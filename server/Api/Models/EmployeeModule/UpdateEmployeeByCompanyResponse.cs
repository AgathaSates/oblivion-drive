namespace OblivionDrive.Api.Models.EmployeeModule;

public record UpdateEmployeeByCompanyResponse
{
    public bool UpdatedSuccessfully { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly HireDate { get; init; }
    public decimal Salary { get; init; }
}