using System.Collections.Immutable;
using OblivionDrive.Application.PartnerModule.DTOs;

namespace OblivionDrive.Api.Models.PartnerModule.Responses;

public record GetAllPartnersResponse(int Quantity, ImmutableList<DetailPartnerDTO> Partners);