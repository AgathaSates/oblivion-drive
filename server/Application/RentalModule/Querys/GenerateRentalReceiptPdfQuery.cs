using FluentResults;
using MediatR;
using OblivionDrive.Application.RentalModule.Results;

namespace OblivionDrive.Application.RentalModule.Querys;

public record GenerateRentalReceiptPdfQuery(Guid RentalId) : IRequest<Result<PdfFileResult>>;
