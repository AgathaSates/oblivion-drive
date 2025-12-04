using FluentValidation;
using OblivionDrive.Application.VehicleGroupModule.commands;

namespace OblivionDrive.Application.FluentValidation.VehicleGroup;
public class RegisterVehicleGroupCommandValidator : AbstractValidator<RegisterVehicleGroupCommand>
{
    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;

    public RegisterVehicleGroupCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
                .WithMessage("O nome do grupo de veículos é obrigatório.")
            .MinimumLength(MinimumNameLength)
                .WithMessage($"O nome do grupo de veículos deve ter pelo menos {MinimumNameLength} caracteres.")
            .MaximumLength(MaximumNameLength)
                .WithMessage($"O nome do grupo de veículos deve ter no máximo {MaximumNameLength} caracteres.");
    }
}