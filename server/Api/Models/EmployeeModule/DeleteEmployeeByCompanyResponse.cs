namespace OblivionDrive.Api.Models.EmployeeModule;

public record DeleteEmployeeByCompanyResponse(
    bool DeletedSuccessfully,
    Guid EmployeeId
);