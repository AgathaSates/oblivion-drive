using FluentResults;
using MediatR;
using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Application.RentalModule.Commands;
public record UpdateRentalCommand(
    Guid RentalId,
    Guid ClientId,
    Guid DriverId,
    Guid VehicleId,
    RentalPlanType PlanType,
    DateOnly StartDate,
    DateOnly ExpectedReturnDate,
    decimal InsuranceDailyPricePerPerson,
    int InsurancePersonsCount,
    int? EstimatedTotalKilometers,
    IReadOnlyCollection<Guid> ServiceIds)
    : IRequest<Result<UpdatedRentalDTO>>;