using System.Collections.Immutable;
using OblivionDrive.Application.BillingPlanModule.DTOs;

namespace OblivionDrive.Api.Models.BillingPlanModule;

public record GetAllBillingPlansResponse(int Quantity, ImmutableList<DetailBillingPlanDTO> BillingPlans);
