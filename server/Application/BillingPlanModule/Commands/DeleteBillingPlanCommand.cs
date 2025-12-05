using FluentResults;
using MediatR;

namespace OblivionDrive.Application.BillingPlanModule.Commands;
public record DeleteBillingPlanCommand(Guid BillingPlanId) : IRequest<Result>;