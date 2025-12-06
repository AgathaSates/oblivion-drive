using FluentValidation;
using OblivionDrive.Application.PartnerModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Partner;
public class GetPartnerByIdQueryValidator : AbstractValidator<GetPartnerByIdQuery>
{
    public GetPartnerByIdQueryValidator()
    {
        RuleFor(q => q.PartnerId)
            .NotEmpty()
                .WithMessage("O identificador do parceiro é obrigatório.");
    }
}