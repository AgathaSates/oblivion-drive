using FluentValidation;
using OblivionDrive.Application.RentalModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Rental;
public sealed class SendRentalReceiptEmailCommandValidator : AbstractValidator<SendRentalReceiptEmailCommand>
{
    public SendRentalReceiptEmailCommandValidator()
    {
        RuleFor(x => x.RentalId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}