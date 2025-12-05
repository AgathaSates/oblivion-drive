namespace OblivionDrive.Api.Models.EmployeeModule.Responses;

public record UpdateOwnEmployeeResponse(
    bool UpdatedSuccessfully,
    string Name);