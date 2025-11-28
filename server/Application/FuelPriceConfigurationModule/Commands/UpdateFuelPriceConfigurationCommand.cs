using FluentResults;
using MediatR;
using OblivionDrive.Application.FuelPriceConfigurationModule.DTOs;

namespace OblivionDrive.Application.FuelPriceConfigurationModule.Commands;

public record UpdateFuelPriceConfigurationCommand(
    decimal Gasoline,
    decimal Gas,
    decimal Diesel,
    decimal Alcohol) : IRequest<Result<FuelPriceConfigurationDto>>;

