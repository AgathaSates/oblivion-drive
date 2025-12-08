using FluentValidation;
using OblivionDrive.Application.RentalModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Rental;

public class DeleteRentalCommandValidator : AbstractValidator<DeleteRentalCommand>
{
    public DeleteRentalCommandValidator()
    {
        RuleFor(c => c.RentalId)
            .NotEmpty()
                .WithMessage("O identificador do aluguel é obrigatório.");
    }
}