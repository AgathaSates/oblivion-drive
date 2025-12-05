using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OblivionDrive.Api.Helpers;
using OblivionDrive.Api.Models.FuelPriceConfigurationModule;
using OblivionDrive.Api.Models.FuelPriceConfigurationModule.Requests;
using OblivionDrive.Api.Models.FuelPriceConfigurationModule.Responses;
using OblivionDrive.Application.FuelPriceConfigurationModule.Commands;
using OblivionDrive.Application.FuelPriceConfigurationModule.Querys;
using Swashbuckle.AspNetCore.Annotations;

namespace OblivionDrive.Api.Controllers;

[ApiController]
[Route("api/fuel-price-configuration")]
[Authorize]
public class FuelPriceConfigurationController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Obter configuração de preços de combustíveis",
        Description = "Retorna a configuração de preços de combustíveis da empresa do usuário logado. Acesso permitido apenas a usuários com o cargo 'Company'."
    )]
    public async Task<ActionResult<GetFuelPriceConfigurationResponse>> Get(CancellationToken cancellationToken)
    {
        var query = new GetFuelPriceConfigurationQuery();

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetFuelPriceConfigurationResponse>(result.Value);

        return Ok(response);
    }

    [HttpPut]
    [SwaggerOperation(
        Summary = "Atualizar configuração de preços de combustíveis",
        Description = "Atualiza os preços de combustíveis da empresa do usuário logado. Acesso permitido apenas a usuários com o cargo 'Company'."
    )]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<UpdateFuelPriceConfigurationResponse>> Update(UpdateFuelPriceConfigurationRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<UpdateFuelPriceConfigurationCommand>(request);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<UpdateFuelPriceConfigurationResponse>(result.Value);

        return Ok(response);
    }
}