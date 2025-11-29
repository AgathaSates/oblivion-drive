using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OblivionDrive.Api.Helpers;
using OblivionDrive.Api.Models.ServicesModule;
using OblivionDrive.Application.ServicesModule.Commands;
using OblivionDrive.Application.ServicesModule.Querys;
using Swashbuckle.AspNetCore.Annotations;

namespace OblivionDrive.Api.Controllers;

[ApiController]
[Route("api/services")]
[Authorize]
public class ServiceController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cadastrar serviço",
        Description = "Cadastra um novo serviço para a empresa do usuário logado."
    )]
    public async Task<ActionResult<RegisterServiceResponse>> Create( RegisterServiceRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<RegisterServiceCommand>(request);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<RegisterServiceResponse>(result.Value);

        return Ok(response);
    }

    [HttpPut("{seviceId:guid}")]
    [SwaggerOperation(
        Summary = "Atualizar serviço",
        Description = "Atualiza os dados de um serviço da empresa do usuário logado."
    )]
    public async Task<ActionResult<UpdateServiceResponse>> Update(Guid seviceId, UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<(Guid, UpdateServiceRequest), UpdateServiceCommand>((seviceId, request));

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<UpdateServiceResponse>(result.Value);

        return Ok(response);
    }

    [HttpDelete("{serviceId:guid}")]
    [SwaggerOperation(
    Summary = "Excluir serviço",
    Description = "Exclui um serviço da empresa do usuário logado."
    )]
    public async Task<ActionResult<DeleteServiceResponse>> Delete(Guid serviceId, CancellationToken cancellationToken)
    {
        var command = new DeleteServiceCommand(serviceId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = new DeleteServiceResponse(true, serviceId);

        return Ok(response);
    }


    [HttpGet("{seviceId:guid}")]
    [SwaggerOperation(
        Summary = "Obter serviço por identificador",
        Description = "Retorna os dados de um serviço pertencente à empresa do usuário logado."
    )]
    public async Task<ActionResult<GetServiceByIdResponse>> GetById(Guid seviceId, CancellationToken cancellationToken)
    {
        var query = new GetServiceByIdQuery(seviceId);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetServiceByIdResponse>(result.Value);

        return Ok(response);
    }

    [HttpGet]
    [SwaggerOperation(
    Summary = "Listar serviços da empresa",
    Description = "Retorna a lista de serviços pertencentes à empresa do usuário logado. Acesso permitido a usuários com os cargos 'Company' ou 'Employee'."
)]
    public async Task<ActionResult<GetAllServicesResponse>> GetAll(int? quantity, CancellationToken cancellationToken)
    {
        var query = new GetAllServicesQuery(quantity);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetAllServicesResponse>(result.Value);

        return Ok(response);
    }
}