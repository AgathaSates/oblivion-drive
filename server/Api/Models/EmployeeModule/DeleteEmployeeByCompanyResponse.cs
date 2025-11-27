namespace OblivionDrive.Api.Models.EmployeeModule;

public record DeleteEmployeeByCompanyResponse
{
    public bool DeletedSuccessfully { get; init; }
    public Guid EmployeeId { get; init; }
}