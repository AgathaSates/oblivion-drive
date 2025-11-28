namespace OblivionDrive.Api.Models.EmployeeModule;

public record UpdateEmployeeByCompanyRequest(
    string Name,
    DateOnly HireDate,
    decimal Salary);