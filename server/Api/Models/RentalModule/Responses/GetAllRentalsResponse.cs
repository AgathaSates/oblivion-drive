using System.Collections.Immutable;

namespace OblivionDrive.Api.Models.RentalModule.Responses;

public record GetAllRentalsResponse(ImmutableList<DetailRentalResponse> Rentals);