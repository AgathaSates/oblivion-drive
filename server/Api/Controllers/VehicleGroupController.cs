using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OblivionDrive.Api.Helpers;
using OblivionDrive.Api.Models.VehicleGroupModule;
using OblivionDrive.Api.Models.VehicleGroupModule.Requests;
using OblivionDrive.Api.Models.VehicleGroupModule.Responses;
using OblivionDrive.Application.VehicleGroupModule.commands;
using OblivionDrive.Application.VehicleGroupModule.Querys;
using Swashbuckle.AspNetCore.Annotations;

namespace OblivionDrive.Api.Controllers;

[ApiController]
[Route("api/vehicle-groups")]
[Authorize]
public class VehicleGroupController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cadastrar grupo de veículos",
        Description = "Cadastra um novo grupo de veículos para a empresa do usuário logado."
    )]
    public async Task<ActionResult<RegisterVehicleGroupResponse>> Create(RegisterVehicleGroupRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<RegisterVehicleGroupCommand>(request);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<RegisterVehicleGroupResponse>(result.Value);

        return Ok(response);
    }

    [HttpPut("{vehicleGroupId:guid}")]
    [SwaggerOperation(
        Summary = "Atualizar grupo de veículos",
        Description = "Atualiza os dados de um grupo de veículos da empresa do usuário logado."
    )]
    public async Task<ActionResult<UpdateVehicleGroupResponse>> Update(
        Guid vehicleGroupId, UpdateVehicleGroupRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<(Guid, UpdateVehicleGroupRequest), UpdateVehicleGroupCommand>((vehicleGroupId, request));

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<UpdateVehicleGroupResponse>(result.Value);

        return Ok(response);
    }

    [HttpDelete("{vehicleGroupId:guid}")]
    [SwaggerOperation(
        Summary = "Excluir grupo de veículos",
        Description = "Exclui um grupo de veículos da empresa do usuário logado."
    )]
    public async Task<ActionResult<DeleteVehicleGroupResponse>> Delete(
        Guid vehicleGroupId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteVehicleGroupCommand(vehicleGroupId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = new DeleteVehicleGroupResponse(true, vehicleGroupId);

        return Ok(response);
    }

    [HttpGet("{vehicleGroupId:guid}")]
    [SwaggerOperation(
        Summary = "Obter grupo de veículos por identificador",
        Description = "Retorna os dados de um grupo de veículos pertencente à empresa do usuário logado."
    )]
    public async Task<ActionResult<GetVehicleGroupByIdResponse>> GetById(
        Guid vehicleGroupId,
        CancellationToken cancellationToken)
    {
        var query = new GetVehicleGroupByIdQuery(vehicleGroupId);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetVehicleGroupByIdResponse>(result.Value);

        return Ok(response);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Listar grupos de veículos da empresa",
        Description = "Retorna a lista de grupos de veículos pertencentes à empresa do usuário logado. Acesso permitido a usuários com os cargos 'Company' ou 'Employee'."
    )]
    public async Task<ActionResult<GetAllVehicleGroupResponse>> GetAll(
        int? quantity,
        CancellationToken cancellationToken)
    {
        var query = new GetAllVehicleGroupQuery(quantity);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetAllVehicleGroupResponse>(result.Value);

        return Ok(response);
    }
}