using FluentResults;
using MediatR;

namespace OblivionDrive.Application.RentalModule.Commands;

public record SendRentalReceiptEmailCommand(Guid RentalId, string Email) : IRequest<Result>;