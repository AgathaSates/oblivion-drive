using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OblivionDrive.Api.Helpers;
using OblivionDrive.Api.Models.BillingPlanModule;
using OblivionDrive.Application.BillingPlanModule.Commands;
using OblivionDrive.Application.BillingPlanModule.Querys;
using Swashbuckle.AspNetCore.Annotations;

namespace OblivionDrive.Api.Controllers;

[ApiController]
[Route("api/billing-plans")]
[Authorize]
public class BillingPlanController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cadastrar plano de cobrança",
        Description = "Cadastra um novo plano de cobrança para a empresa do usuário logado."
    )]
    public async Task<ActionResult<RegisterBillingPlanResponse>> Create(RegisterBillingPlanRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<RegisterBillingPlanCommand>(request);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<RegisterBillingPlanResponse>(result.Value);

        return Ok(response);
    }

    [HttpPut("{billingPlanId:guid}")]
    [SwaggerOperation(
        Summary = "Atualizar plano de cobrança",
        Description = "Atualiza os dados de um plano de cobrança da empresa do usuário logado."
    )]
    public async Task<ActionResult<UpdateBillingPlanResponse>> Update(
        Guid billingPlanId, UpdateBillingPlanRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<(Guid, UpdateBillingPlanRequest), UpdateBillingPlanCommand>(
            (billingPlanId, request));

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<UpdateBillingPlanResponse>(result.Value);

        return Ok(response);
    }

    [HttpDelete("{billingPlanId:guid}")]
    [SwaggerOperation(
        Summary = "Excluir plano de cobrança",
        Description = "Exclui um plano de cobrança da empresa do usuário logado."
    )]
    public async Task<ActionResult<DeleteBillingPlanResponse>> Delete(Guid billingPlanId, CancellationToken cancellationToken)
    {
        var command = new DeleteBillingPlanCommand(billingPlanId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = new DeleteBillingPlanResponse(true, billingPlanId);

        return Ok(response);
    }

    [HttpGet("{billingPlanId:guid}")]
    [SwaggerOperation(
        Summary = "Obter plano de cobrança por identificador",
        Description = "Retorna os dados de um plano de cobrança pertencente à empresa do usuário logado."
    )]
    public async Task<ActionResult<GetBillingPlanByIdResponse>> GetById(Guid billingPlanId, CancellationToken cancellationToken)
    {
        var query = new GetBillingPlanByIdQuery(billingPlanId);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetBillingPlanByIdResponse>(result.Value);

        return Ok(response);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Listar planos de cobrança da empresa",
        Description = "Retorna a lista de planos de cobrança pertencentes à empresa do usuário logado. Acesso permitido a usuários com os cargos 'Company' ou 'Employee'."
    )]
    public async Task<ActionResult<GetAllBillingPlansResponse>> GetAll(int? quantity, CancellationToken cancellationToken)
    {
        var query = new GetAllBillingPlanQuery(quantity);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetAllBillingPlansResponse>(result.Value);

        return Ok(response);
    }
}