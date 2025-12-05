using FluentValidation;
using OblivionDrive.Application.DriverModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Driver;
public class DeleteDriverCommandValidator : AbstractValidator<DeleteDriverCommand>
{
    public DeleteDriverCommandValidator()
    {
        RuleFor(c => c.DriverId)
            .NotEmpty()
                .WithMessage("O identificador do condutor é obrigatório.");
    }
}