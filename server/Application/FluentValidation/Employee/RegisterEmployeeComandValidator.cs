using FluentValidation;
using OblivionDrive.Application.EmployeeModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Employee;
public class RegisterEmployeeComandValidator : AbstractValidator<RegisterEmployeeCommand>
{
    private const int MinimumPasswordLength = 6;
    private const string EmailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    private const string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{6,}$";
    private const string UsernameRegex = @"^\S+$";
    private const string NameRegex =  @"^[A-Za-zÀ-ÖØ-öø-ÿ\s]+$";
    private DateOnly minimumHireDate = new DateOnly(1970, 1, 1);
    private decimal MaximumSalary = 1_000_000m;

    public RegisterEmployeeComandValidator()
    {
        RuleFor(c => c.UserName)
            .NotEmpty()
                .WithMessage("O nome de usuário é obrigatório.")
            .MinimumLength(3)
                .WithMessage("O nome de usuário deve ter pelo menos 3 caracteres.")
            .MaximumLength(100)
                .WithMessage("O nome de usuário deve ter no máximo 100 caracteres.")
            .Matches(UsernameRegex)
                .WithMessage("O nome de usuário não deve conter espaços em branco.");

        RuleFor(c => c.Email)
             .NotEmpty()
                .WithMessage("O e-mail é obrigatório.")
            .MaximumLength(256)
                .WithMessage("O e-mail deve ter no máximo 256 caracteres.")
             .Matches(EmailRegex)
                 .WithMessage("O e-mail deve estar no formato [ nome@dominio.com ].");

        RuleFor(c => c.Password)
            .NotEmpty()
                .WithMessage("A senha é obrigatória.")
            .MinimumLength(MinimumPasswordLength)
                .WithMessage($"A senha deve ter pelo menos {MinimumPasswordLength} caracteres.")
            .MaximumLength(100)
                .WithMessage("A senha deve ter no máximo 100 caracteres.")
            .Matches(PasswordRegex)
                .WithMessage("A senha deve conter pelo menos uma letra maiúscula, uma letra minúscula, um número e um caractere especial.");

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
            .NotEmpty().WithMessage("A data de contratação é obrigatória.")
            .GreaterThanOrEqualTo(minimumHireDate)
                .WithMessage($"A data de contratação não pode ser anterior a {minimumHireDate:dd/MM/yyyy}.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("A data de contratação não pode ser uma data futura.");

        RuleFor(c => c.Salary)
            .GreaterThan(0).WithMessage("O salário deve ser maior que zero.")
            .LessThanOrEqualTo(MaximumSalary)
                .WithMessage($"O salário não pode ser maior que {MaximumSalary:N2}.");
    }
}