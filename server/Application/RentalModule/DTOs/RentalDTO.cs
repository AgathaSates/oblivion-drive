namespace OblivionDrive.Application.RentalModule.DTOs;

public record RentalDTO(
    bool CreatedSuccessfully,
    Guid RentalId,
    decimal EstimatedRentalAmount);