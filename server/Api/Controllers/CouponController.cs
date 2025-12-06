using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OblivionDrive.Api.Helpers;
using OblivionDrive.Api.Models.CouponModule.Requests;
using OblivionDrive.Api.Models.CouponModule.Responses;
using OblivionDrive.Application.CouponModule.Commands;
using OblivionDrive.Application.CouponModule.Querys;
using Swashbuckle.AspNetCore.Annotations;

namespace OblivionDrive.Api.Controllers;

[ApiController]
[Route("api/coupons")]
[Authorize]
public class CouponController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cadastrar cupom",
        Description = "Cadastra um novo cupom para a empresa do usuário logado, vinculado a um parceiro."
    )]
    public async Task<ActionResult<RegisterCouponResponse>> Create(RegisterCouponRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<RegisterCouponCommand>(request);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<RegisterCouponResponse>(result.Value);

        return Ok(response);
    }

    [HttpPut("{couponId:guid}")]
    [SwaggerOperation(
        Summary = "Atualizar cupom",
        Description = "Atualiza os dados de um cupom da empresa do usuário logado."
    )]
    public async Task<ActionResult<UpdateCouponResponse>> Update(
        Guid couponId, UpdateCouponRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<(Guid, UpdateCouponRequest), UpdateCouponCommand>((couponId, request));

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<UpdateCouponResponse>(result.Value);

        return Ok(response);
    }

    [HttpDelete("{couponId:guid}")]
    [SwaggerOperation(
        Summary = "Excluir cupom",
        Description = "Exclui um cupom da empresa do usuário logado."
    )]
    public async Task<ActionResult<DeleteCouponResponse>> Delete(Guid couponId, CancellationToken cancellationToken)
    {
        var command = new DeleteCouponCommand(couponId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = new DeleteCouponResponse(true, couponId);

        return Ok(response);
    }

    [HttpGet("{couponId:guid}")]
    [SwaggerOperation(
        Summary = "Obter cupom por identificador",
        Description = "Retorna os dados de um cupom pertencente à empresa do usuário logado."
    )]
    public async Task<ActionResult<GetCouponByIdResponse>> GetById(Guid couponId, CancellationToken cancellationToken)
    {
        var query = new GetCouponByIdQuery(couponId);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetCouponByIdResponse>(result.Value);

        return Ok(response);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Listar cupons da empresa",
        Description = "Retorna a lista de cupons pertencentes à empresa do usuário logado. " +
                      "Permite limitar a quantidade de registros retornados."
    )]
    public async Task<ActionResult<GetAllCouponsResponse>> GetAll(int? quantity, CancellationToken cancellationToken)
    {
        var query = new GetAllCouponsQuery(quantity);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetAllCouponsResponse>(result.Value);

        return Ok(response);
    }
}