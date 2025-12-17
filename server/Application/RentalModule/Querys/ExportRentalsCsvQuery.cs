using FluentResults;
using MediatR;

namespace OblivionDrive.Application.RentalModule.Querys;
public record ExportRentalsCsvQuery(int? Quantity) : IRequest<Result<(byte[] Content, string FileName)>>;
