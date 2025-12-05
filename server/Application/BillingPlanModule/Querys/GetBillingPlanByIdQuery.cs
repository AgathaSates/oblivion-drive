using FluentResults;
using MediatR;
using OblivionDrive.Application.BillingPlanModule.DTOs;

namespace OblivionDrive.Application.BillingPlanModule.Querys;
public record GetBillingPlanByIdQuery(Guid BillingPlanId) : IRequest<Result<DetailBillingPlanDTO>>;