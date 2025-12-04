using FluentValidation;
using OblivionDrive.Application.VehicleGroupModule.commands;

namespace OblivionDrive.Application.FluentValidation.VehicleGroup;
public class DeleteVehicleGroupCommandValidator : AbstractValidator<DeleteVehicleGroupCommand>
{
    public DeleteVehicleGroupCommandValidator()
    {
        RuleFor(c => c.VehicleGroupId)
            .NotEmpty()
                .WithMessage("O identificador do grupo de veículos é obrigatório.");
    }
}