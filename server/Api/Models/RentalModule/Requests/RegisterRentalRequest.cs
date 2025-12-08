using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Api.Models.RentalModule.Requests;

public record RegisterRentalRequest(
    Guid ClientId,
    Guid DriverId,
    Guid VehicleId,
    RentalPlanType PlanType,
    DateOnly StartDate,
    DateOnly ExpectedReturnDate,
    decimal InsuranceDailyPricePerPerson,
    int InsurancePersonsCount,
    int? EstimatedTotalKilometers,
    IReadOnlyCollection<Guid>? ServiceIds
);