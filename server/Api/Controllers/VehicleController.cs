using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OblivionDrive.Api.Helpers;
using OblivionDrive.Api.Models.VehicleModule.Requests;
using OblivionDrive.Api.Models.VehicleModule.Responses;
using OblivionDrive.Application.VehicleModule.Commands;
using OblivionDrive.Application.VehicleModule.Querys;
using Swashbuckle.AspNetCore.Annotations;

namespace OblivionDrive.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize]
public class VehicleController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cadastrar veículo",
        Description = "Cadastra um novo veículo para a empresa do usuário logado, incluindo a foto."
    )]
    public async Task<ActionResult<RegisterVehicleResponse>> Create(RegisterVehicleRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<RegisterVehicleCommand>(request);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<RegisterVehicleResponse>(result.Value);

        return Ok(response);
    }

    [HttpPut("{vehicleId:guid}")]
    [SwaggerOperation(
        Summary = "Atualizar veículo",
        Description = "Atualiza os dados de um veículo da empresa do usuário logado. Permite também alterar a foto."
    )]
    public async Task<ActionResult<UpdateVehicleResponse>> Update(Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<(Guid, UpdateVehicleRequest), UpdateVehicleCommand>(
            (vehicleId, request));

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<UpdateVehicleResponse>(result.Value);

        return Ok(response);
    }

    [HttpDelete("{vehicleId:guid}")]
    [SwaggerOperation(
        Summary = "Excluir veículo",
        Description = "Exclui um veículo da empresa do usuário logado. Não deve ser possível excluir veículos com aluguel ainda não concluído."
    )]
    public async Task<ActionResult<DeleteVehicleResponse>> Delete(Guid vehicleId, CancellationToken cancellationToken)
    {
        var command = new DeleteVehicleCommand(vehicleId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = new DeleteVehicleResponse(true, vehicleId);

        return Ok(response);
    }

    [HttpGet("{vehicleId:guid}")]
    [SwaggerOperation(
        Summary = "Obter veículo por identificador",
        Description = "Retorna os dados de um veículo pertencente à empresa do usuário logado."
    )]
    public async Task<ActionResult<GetVehicleByIdResponse>> GetById(Guid vehicleId, CancellationToken cancellationToken)
    {
        var query = new GetVehicleByIdQuery(vehicleId);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetVehicleByIdResponse>(result.Value);

        return Ok(response);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Listar veículos da empresa",
        Description = "Retorna a lista de veículos pertencentes à empresa do usuário logado. " +
                      "Permite filtrar por grupo de veículos e limitar a quantidade de registros retornados."
    )]
    public async Task<ActionResult<GetAllVehiclesResponse>> GetAll(Guid? vehicleGroupId, int? quantity, CancellationToken cancellationToken)
    {
        var query = new GetAllVehiclesQuery(vehicleGroupId, quantity);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetAllVehiclesResponse>(result.Value);

        return Ok(response);
    }
}
