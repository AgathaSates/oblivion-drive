using FluentResults;
using MediatR;
using OblivionDrive.Application.VehicleModule.DTOs;
using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Application.VehicleModule.Commands;
public record UpdateVehicleCommand(
    Guid VehicleId,
    string Brand,
    string Model,
    string Color,
    FuelType FuelType,
    decimal FuelTankCapacityInLiters,
    int Year,
    Guid VehicleGroupId,
    byte[]? PhotoBytes
) : IRequest<Result<UpdatedVehicleDTO>>;