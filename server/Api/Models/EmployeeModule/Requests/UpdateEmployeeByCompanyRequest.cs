namespace OblivionDrive.Api.Models.EmployeeModule.Requests;

public record UpdateEmployeeByCompanyRequest(
    string Name,
    DateOnly HireDate,
    decimal Salary);