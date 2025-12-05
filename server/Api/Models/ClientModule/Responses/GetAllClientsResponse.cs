using System.Collections.Immutable;
using OblivionDrive.Application.ClientModule.DTOs;

namespace OblivionDrive.Api.Models.ClientModule.Responses;

public record GetAllClientsResponse(int Quantity, ImmutableList<DetailClientDTO> Clients);