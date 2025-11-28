namespace OblivionDrive.Api.Models.EmployeeModule;

public record GetEmployeeByCompanyResponse(
    Guid Id ,
    string Name,
    DateOnly HireDate,
    decimal Salary);