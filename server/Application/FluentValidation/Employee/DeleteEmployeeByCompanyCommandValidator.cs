using FluentValidation;
using OblivionDrive.Application.EmployeeModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Employee;
public class DeleteEmployeeByCompanyCommandValidator : AbstractValidator<DeleteEmployeeByCompanyCommand>
{
    public DeleteEmployeeByCompanyCommandValidator()
    {
        RuleFor(c => c.EmployeeId)
            .NotEmpty()
                .WithMessage("O identificador do funcionário é obrigatório.");
    }
}