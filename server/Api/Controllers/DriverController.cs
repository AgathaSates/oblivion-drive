using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OblivionDrive.Api.Helpers;
using OblivionDrive.Api.Models.DriverModule.Requests;
using OblivionDrive.Api.Models.DriverModule.Responses;
using OblivionDrive.Application.DriverModule.Commands;
using OblivionDrive.Application.DriverModule.Querys;
using Swashbuckle.AspNetCore.Annotations;

namespace OblivionDrive.Api.Controllers;

[ApiController]
[Route("api/drivers")]
[Authorize]
public class DriverController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cadastrar condutor",
        Description = "Cadastra um novo condutor vinculado a um cliente da empresa do usuário logado."
    )]
    public async Task<ActionResult<RegisterDriverResponse>> Create(RegisterDriverRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<RegisterDriverCommand>(request);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<RegisterDriverResponse>(result.Value);

        return Ok(response);
    }

    [HttpPut("{driverId:guid}")]
    [SwaggerOperation(
        Summary = "Atualizar condutor",
        Description = "Atualiza os dados de um condutor vinculado a um cliente da empresa do usuário logado."
    )]
    public async Task<ActionResult<UpdateDriverResponse>> Update(Guid driverId, UpdateDriverRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<(Guid, UpdateDriverRequest), UpdateDriverCommand>(
            (driverId, request));

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<UpdateDriverResponse>(result.Value);

        return Ok(response);
    }

    [HttpDelete("{driverId:guid}")]
    [SwaggerOperation(
        Summary = "Excluir condutor",
        Description = "Exclui um condutor da empresa do usuário logado. " +
                      "Não deve ser possível excluir condutores relacionados a aluguéis ainda não concluídos."
    )]
    public async Task<ActionResult<DeleteDriverResponse>> Delete(Guid driverId, CancellationToken cancellationToken)
    {
        var command = new DeleteDriverCommand(driverId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = new DeleteDriverResponse(true, driverId);

        return Ok(response);
    }

    [HttpGet("{driverId:guid}")]
    [SwaggerOperation(
        Summary = "Obter condutor por identificador",
        Description = "Retorna os dados de um condutor pertencente à empresa do usuário logado."
    )]
    public async Task<ActionResult<GetDriverByIdResponse>> GetById(Guid driverId, CancellationToken cancellationToken)
    {
        var query = new GetDriverByIdQuery(driverId);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetDriverByIdResponse>(result.Value);

        return Ok(response);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Listar condutores da empresa",
        Description = "Retorna a lista de condutores pertencentes à empresa do usuário logado. " +
                      "Permite limitar a quantidade de registros retornados."
    )]
    public async Task<ActionResult<GetAllDriversResponse>> GetAll(int? quantity, CancellationToken cancellationToken)
    {
        var query = new GetAllDriversQuery(quantity);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetAllDriversResponse>(result.Value);

        return Ok(response);
    }
}