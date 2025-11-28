using FluentResults;
using MediatR;
using OblivionDrive.Application.FuelPriceConfigurationModule.DTOs;

namespace OblivionDrive.Application.FuelPriceConfigurationModule.Querys;
public record GetFuelPriceConfigurationQuery() : IRequest<Result<FuelPriceConfigurationDto>>;