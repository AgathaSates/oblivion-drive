using FluentValidation;
using OblivionDrive.Application.EmployeeModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Employee;
public class UpdateEmployeeByCompanyCommandValidator : AbstractValidator<UpdateEmployeeByCompanyCommand>
{
    private const string NameRegex = @"^[A-Za-zÀ-ÖØ-öø-ÿ\s]+$";
    private DateOnly minimumHireDate = new DateOnly(1970, 1, 1);
    private decimal MaximumSalary = 1_000_000m;

    public UpdateEmployeeByCompanyCommandValidator()
    {
        RuleFor(c => c.EmployeeId)
            .NotEmpty()
                .WithMessage("O identificador do funcionário é obrigatório.");

        RuleFor(c => c.Name)
            .NotEmpty()
                .WithMessage("O nome do funcionário é obrigatório.")
            .MinimumLength(2)
                .WithMessage("O nome do funcionário deve ter pelo menos 2 caracteres.")
            .MaximumLength(200)
                .WithMessage("O nome do funcionário deve ter no máximo 200 caracteres.")
            .Matches(NameRegex)
                .WithMessage("O nome do funcionário deve conter apenas letras e espaços.");

        RuleFor(c => c.HireDate)
            .NotEmpty()
                .WithMessage("A data de contratação é obrigatória.")
            .GreaterThanOrEqualTo(minimumHireDate)
                .WithMessage($"A data de contratação não pode ser anterior a {minimumHireDate:dd/MM/yyyy}.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("A data de contratação não pode ser uma data futura.");

        RuleFor(c => c.Salary)
            .GreaterThan(0)
                .WithMessage("O salário deve ser maior que zero.")
            .LessThanOrEqualTo(MaximumSalary)
                .WithMessage($"O salário não pode ser maior que {MaximumSalary:N2}.");
    }
}
