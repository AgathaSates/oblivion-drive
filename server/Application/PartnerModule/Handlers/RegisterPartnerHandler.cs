using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.PartnerModule.Commands;
using OblivionDrive.Application.PartnerModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.PartnerModule.Handlers;
public class RegisterPartnerHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryPartner partnerRepository,
    IUnitOfWork unitOfWork, IValidator<RegisterPartnerCommand> validator, ILogger<RegisterPartnerCommand> logger,
    IMapper mapper) : IRequestHandler<RegisterPartnerCommand, Result<PartnerDTO>>
{
    public async Task<Result<PartnerDTO>> Handle(RegisterPartnerCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            List<string> validationErrors = validationResult.Errors
                .Select(error => error.ErrorMessage)
                .ToList();

            return Result.Fail(ErrorResults.InvalidRequestError(validationErrors));
        }

        if (tenantProvider.UserId is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário não está autenticado."));

        Guid currentUserId = tenantProvider.UserId.Value;

        User? currentUser = await userManager.FindByIdAsync(currentUserId.ToString());

        if (currentUser is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid companyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            string formattedName = NameFormatter.FormatName(command.Name);

            bool partnerNameAlreadyExists = await partnerRepository.ExistsByNameAsync(formattedName);

            if (partnerNameAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um parceiro cadastrado com este nome para esta empresa."));          

            Partner partner = new(formattedName, companyId);

            await partnerRepository.AddAsync(partner);
            await unitOfWork.CommitAsync();

            PartnerDTO partnerDto = mapper.Map<PartnerDTO>(partner);

            return Result.Ok(partnerDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante o registro de parceiro {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}