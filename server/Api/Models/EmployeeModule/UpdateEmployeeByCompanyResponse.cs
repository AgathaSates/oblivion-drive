namespace OblivionDrive.Api.Models.EmployeeModule;

public record UpdateEmployeeByCompanyResponse(
    bool UpdatedSuccessfully, 
    string Name, 
    DateOnly HireDate,
    decimal Salary);