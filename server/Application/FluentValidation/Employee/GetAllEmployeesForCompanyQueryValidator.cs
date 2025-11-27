using FluentValidation;
using OblivionDrive.Application.EmployeeModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Employee;

public class GetAllEmployeesForCompanyQueryValidator
    : AbstractValidator<GetAllEmployeesForCompanyQuery>
{
    public GetAllEmployeesForCompanyQueryValidator()
    {
        RuleFor(q => q.Quantity)
            .GreaterThan(0)
            .When(q => q.Quantity.HasValue)
            .WithMessage("A quantidade deve ser maior que zero.");
    }
}