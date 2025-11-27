namespace OblivionDrive.Api.Models.EmployeeModule;

public record RegisterEmployeeRequest(
    string UserName,
    string Email,
    string Password,
    string Name,
    DateOnly HireDate,
    Decimal Salary);
