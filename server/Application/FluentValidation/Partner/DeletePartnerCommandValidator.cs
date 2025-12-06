
using FluentValidation;
using OblivionDrive.Application.PartnerModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Partner;
public class DeletePartnerCommandValidator : AbstractValidator<DeletePartnerCommand>
{
    public DeletePartnerCommandValidator()
    {
        RuleFor(c => c.PartnerId)
            .NotEmpty()
                .WithMessage("O identificador do parceiro é obrigatório.");
    }
}