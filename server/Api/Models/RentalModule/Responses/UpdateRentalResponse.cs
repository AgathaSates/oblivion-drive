using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Api.Models.RentalModule.Responses;

public record UpdateRentalResponse(
    bool UpdatedSuccessfully,
    Guid RentalId,
    decimal EstimatedRentalAmount);