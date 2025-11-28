namespace OblivionDrive.Application.EmployeeModule.DTOs;
public record UpdatedEmployeeDTO(
    bool UpdatedSuccessfully,
    string Name,
    DateOnly HireDate,
    decimal Salary);