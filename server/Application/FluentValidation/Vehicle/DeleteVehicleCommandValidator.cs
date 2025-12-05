using FluentValidation;
using OblivionDrive.Application.VehicleModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Vehicle;
public class DeleteVehicleCommandValidator : AbstractValidator<DeleteVehicleCommand>
{
    public DeleteVehicleCommandValidator()
    {
        RuleFor(c => c.VehicleId)
            .NotEmpty()
                .WithMessage("O identificador do veículo é obrigatório.");
    }
}
