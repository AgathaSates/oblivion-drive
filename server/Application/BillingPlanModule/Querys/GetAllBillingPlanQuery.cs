using System.Collections.Immutable;
using FluentResults;
using MediatR;
using OblivionDrive.Application.BillingPlanModule.DTOs;

namespace OblivionDrive.Application.BillingPlanModule.Querys;
public record GetAllBillingPlanQuery(int? Quantity) : IRequest<Result<BillingPlanResult>>;

public record BillingPlanResult(ImmutableList<DetailBillingPlanDTO> BillingPlans);