using FluentValidation;
using OblivionDrive.Application.EmployeeModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Employee;
public class UpdateOwnEmployeeProfileCommandValidator : AbstractValidator<UpdateOwnEmployeeProfileCommand>
{
    private const string NameRegex = @"^[A-Za-zÀ-ÖØ-öø-ÿ\s]+$";

    public UpdateOwnEmployeeProfileCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
                .WithMessage("O nome do funcionário é obrigatório.")
            .MinimumLength(2)
                .WithMessage("O nome do funcionário deve ter pelo menos 2 caracteres.")
            .MaximumLength(200)
                .WithMessage("O nome do funcionário deve ter no máximo 200 caracteres.")
            .Matches(NameRegex)
                .WithMessage("O nome do funcionário deve conter apenas letras e espaços.");
    }
}
