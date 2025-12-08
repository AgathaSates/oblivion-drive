namespace OblivionDrive.Application.RentalModule.DTOs;

public record UpdatedRentalDTO(
    bool UpdatedSuccessfully,
    Guid RentalId,
    decimal EstimatedRentalAmount);