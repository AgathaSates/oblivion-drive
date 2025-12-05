namespace OblivionDrive.Api.Models.EmployeeModule.Requests;

public record RegisterEmployeeRequest(
    string UserName,
    string Email,
    string Password,
    string Name,
    DateOnly HireDate,
    decimal Salary);
