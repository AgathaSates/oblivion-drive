using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OblivionDrive.Api.Helpers;
using OblivionDrive.Api.Models.ClientModule.Requests;
using OblivionDrive.Api.Models.ClientModule.Responses;
using OblivionDrive.Application.ClientModule.Commands;
using OblivionDrive.Application.ClientModule.Querys;
using Swashbuckle.AspNetCore.Annotations;

namespace OblivionDrive.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cadastrar cliente",
        Description = "Cadastra um novo cliente (Pessoa Física ou Jurídica) para a empresa do usuário logado."
    )]
    public async Task<ActionResult<RegisterClientResponse>> Create(RegisterClientRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<RegisterClientCommand>(request);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<RegisterClientResponse>(result.Value);

        return Ok(response);
    }

    [HttpPut("{clientId:guid}")]
    [SwaggerOperation(
        Summary = "Atualizar cliente",
        Description = "Atualiza os dados de um cliente (Pessoa Física ou Jurídica) da empresa do usuário logado."
    )]
    public async Task<ActionResult<UpdateClientResponse>> Update(Guid clientId, UpdateClientRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<(Guid, UpdateClientRequest), UpdateClientCommand>(
            (clientId, request));

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<UpdateClientResponse>(result.Value);

        return Ok(response);
    }

    [HttpDelete("{clientId:guid}")]
    [SwaggerOperation(
        Summary = "Excluir cliente",
        Description = "Exclui um cliente da empresa do usuário logado. " +
                      "Não deve ser possível excluir clientes relacionados a aluguéis ainda não concluídos."
    )]
    public async Task<ActionResult<DeleteClientResponse>> Delete(Guid clientId, CancellationToken cancellationToken)
    {
        var command = new DeleteClientCommand(clientId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = new DeleteClientResponse(true, clientId);

        return Ok(response);
    }

    [HttpGet("{clientId:guid}")]
    [SwaggerOperation(
        Summary = "Obter cliente por identificador",
        Description = "Retorna os dados de um cliente pertencente à empresa do usuário logado."
    )]
    public async Task<ActionResult<GetClientByIdResponse>> GetById(Guid clientId, CancellationToken cancellationToken)
    {
        var query = new GetClientByIdQuery(clientId);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetClientByIdResponse>(result.Value);

        return Ok(response);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Listar clientes da empresa",
        Description = "Retorna a lista de clientes pertencentes à empresa do usuário logado. " +
                      "Permite limitar a quantidade de registros retornados."
    )]
    public async Task<ActionResult<GetAllClientsResponse>> GetAll(int? quantity, CancellationToken cancellationToken)
    {
        var query = new GetAllClientsQuery(quantity);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetAllClientsResponse>(result.Value);

        return Ok(response);
    }
}
