using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OblivionDrive.Api.Helpers;
using OblivionDrive.Api.Models.PartnerModule.Requests;
using OblivionDrive.Api.Models.PartnerModule.Responses;
using OblivionDrive.Application.PartnerModule.Commands;
using OblivionDrive.Application.PartnerModule.Querys;
using Swashbuckle.AspNetCore.Annotations;

namespace OblivionDrive.Api.Controllers;

[ApiController]
[Route("api/partners")]
[Authorize]
public class PartnerController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cadastrar parceiro",
        Description = "Cadastra um novo parceiro para a empresa do usuário logado."
    )]
    public async Task<ActionResult<RegisterPartnerResponse>> Create(RegisterPartnerRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<RegisterPartnerCommand>(request);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<RegisterPartnerResponse>(result.Value);

        return Ok(response);
    }

    [HttpPut("{partnerId:guid}")]
    [SwaggerOperation(
        Summary = "Atualizar parceiro",
        Description = "Atualiza os dados de um parceiro da empresa do usuário logado."
    )]
    public async Task<ActionResult<UpdatePartnerResponse>> Update(Guid partnerId, UpdatePartnerRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<(Guid, UpdatePartnerRequest), UpdatePartnerCommand>((partnerId, request));

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<UpdatePartnerResponse>(result.Value);

        return Ok(response);
    }

    [HttpDelete("{partnerId:guid}")]
    [SwaggerOperation(
        Summary = "Excluir parceiro",
        Description = "Exclui um parceiro da empresa do usuário logado. Não deve ser possível excluir parceiros vinculados a cupons."
    )]
    public async Task<ActionResult<DeletePartnerResponse>> Delete(Guid partnerId, CancellationToken cancellationToken)
    {
        var command = new DeletePartnerCommand(partnerId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = new DeletePartnerResponse(true, partnerId);

        return Ok(response);
    }

    [HttpGet("{partnerId:guid}")]
    [SwaggerOperation(
        Summary = "Obter parceiro por identificador",
        Description = "Retorna os dados de um parceiro pertencente à empresa do usuário logado."
    )]
    public async Task<ActionResult<GetPartnerByIdResponse>> GetById(Guid partnerId, CancellationToken cancellationToken)
    {
        var query = new GetPartnerByIdQuery(partnerId);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetPartnerByIdResponse>(result.Value);

        return Ok(response);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Listar parceiros da empresa",
        Description = "Retorna a lista de parceiros pertencentes à empresa do usuário logado. " +
                      "Permite limitar a quantidade de registros retornados."
    )]
    public async Task<ActionResult<GetAllPartnersResponse>> GetAll(int? quantity, CancellationToken cancellationToken)
    {
        var query = new GetAllPartnersQuery(quantity);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetAllPartnersResponse>(result.Value);

        return Ok(response);
    }
}