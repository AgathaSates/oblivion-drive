using FluentValidation;
using OblivionDrive.Application.RentalModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Rental;
public class SendRentalReceiptEmailCommandValidator : AbstractValidator<SendRentalReceiptEmailCommand>
{
    public SendRentalReceiptEmailCommandValidator()
    {
        RuleFor(x => x.RentalId)
           .NotEmpty()
           .WithMessage("O identificador do aluguel é obrigatório.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("O e-mail é obrigatório.")
            .EmailAddress()
            .WithMessage("O e-mail informado é inválido.");
    }
}