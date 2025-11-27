using FluentValidation;
using OblivionDrive.Application.EmployeeModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Employee;
public class GetEmployeeByIdForCompanyQueryValidator : AbstractValidator<GetEmployeeByIdForCompanyQuery>
{
    public GetEmployeeByIdForCompanyQueryValidator()
    {
        RuleFor(q => q.EmployeeId)
            .NotEmpty()
                .WithMessage("O identificador do funcionário é obrigatório.");
    }
}