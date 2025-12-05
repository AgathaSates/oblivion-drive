namespace OblivionDrive.Api.Models.EmployeeModule.Responses;

public record DeleteEmployeeByCompanyResponse(
    bool DeletedSuccessfully,
    Guid EmployeeId
);