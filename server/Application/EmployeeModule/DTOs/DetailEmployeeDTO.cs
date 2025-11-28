namespace OblivionDrive.Application.EmployeeModule.DTOs;
public record DetailEmployeeDTO(
    Guid Id,
    string Name,
    DateOnly HireDate,
    decimal Salary);