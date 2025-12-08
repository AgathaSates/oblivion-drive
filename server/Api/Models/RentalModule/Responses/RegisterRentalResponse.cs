using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Api.Models.RentalModule.Responses;

public record RegisterRentalResponse(
    bool CreatedSuccessfully,
    Guid RentalId,
    decimal EstimatedRentalAmount);