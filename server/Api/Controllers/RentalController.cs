using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OblivionDrive.Api.Helpers;
using OblivionDrive.Api.Models.RentalModule.Requests;
using OblivionDrive.Api.Models.RentalModule.Responses;
using OblivionDrive.Application.RentalModule.Commands;
using OblivionDrive.Application.RentalModule.Querys;
using Swashbuckle.AspNetCore.Annotations;

namespace OblivionDrive.Api.Controllers;

[ApiController]
[Route("api/rentals")]
[Authorize]
public class RentalController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cadastrar aluguel",
        Description = "Cadastra um novo aluguel para a empresa do usuário logado."
    )]
    public async Task<ActionResult<RegisterRentalResponse>> Create(
        RegisterRentalRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<RegisterRentalCommand>(request);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<RegisterRentalResponse>(result.Value);

        return Ok(response);
    }

    [HttpPut("{rentalId:guid}")]
    [SwaggerOperation(
        Summary = "Atualizar aluguel",
        Description = "Atualiza os dados de um aluguel da empresa do usuário logado."
    )]
    public async Task<ActionResult<UpdateRentalResponse>> Update(
        Guid rentalId, UpdateRentalRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<(Guid, UpdateRentalRequest), UpdateRentalCommand>((rentalId, request));

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<UpdateRentalResponse>(result.Value);

        return Ok(response);
    }

    [HttpDelete("{rentalId:guid}")]
    [SwaggerOperation(
        Summary = "Excluir aluguel",
        Description = "Exclui um aluguel da empresa do usuário logado."
    )]
    public async Task<ActionResult<DeleteRentalResponse>> Delete(
        Guid rentalId, CancellationToken cancellationToken)
    {
        var command = new DeleteRentalCommand(rentalId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = new DeleteRentalResponse(true, rentalId);

        return Ok(response);
    }

    [HttpGet("{rentalId:guid}")]
    [SwaggerOperation(
        Summary = "Obter aluguel por identificador",
        Description = "Retorna os dados de um aluguel pertencente à empresa do usuário logado."
    )]
    public async Task<ActionResult<GetRentalByIdResponse>> GetById(
        Guid rentalId, CancellationToken cancellationToken)
    {
        var query = new GetRentalByIdQuery(rentalId);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetRentalByIdResponse>(result.Value);

        return Ok(response);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Listar aluguéis da empresa",
        Description = "Retorna a lista de aluguéis pertencentes à empresa do usuário logado. Permite limitar a quantidade de registros retornados."
    )]
    public async Task<ActionResult<GetAllRentalsResponse>> GetAll(
        int? quantity, CancellationToken cancellationToken)
    {
        var query = new GetAllRentalsQuery(quantity);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<GetAllRentalsResponse>(result.Value);

        return Ok(response);
    }

    [HttpPost("{rentalId:guid}/return")]
    [SwaggerOperation(
        Summary = "Registrar devolução de aluguel",
        Description = "Registra a devolução do veículo, calcula preço final, multa por atraso, taxa de combustível e desconto de cupom."
    )]
    public async Task<ActionResult<CompleteRentalReturnResponse>> CompleteReturn(
        Guid rentalId, CompleteRentalReturnRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<(Guid, CompleteRentalReturnRequest), CompleteRentalReturnCommand>((rentalId, request));

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = mapper.Map<CompleteRentalReturnResponse>(result.Value);

        return Ok(response);
    }

    [HttpGet("{rentalId:guid}/receipt")]
    [SwaggerOperation(
    Summary = "Gerar recibo (PDF) de aluguel encerrado",
    Description = "Gera um PDF estilo nota/recibo para um aluguel concluído."
    )]
    public async Task<IActionResult> GetReceiptPdf(Guid rentalId, CancellationToken cancellationToken)
    {
        var query = new GenerateRentalReceiptPdfQuery(rentalId);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");

        return File(
            fileContents: result.Value.Content,
            contentType: "application/pdf",
            fileDownloadName: result.Value.FileName
        );
    }

    [HttpPost("{rentalId:guid}/receipt/email")]
    [EnableRateLimiting("RentalReceiptEmailPolicy")]
    public async Task<IActionResult> SendReceiptByEmail(Guid rentalId, SendRentalReceiptEmailRequest request, CancellationToken cancellationToken)
    {
        var command = new SendRentalReceiptEmailCommand(rentalId, request.Email);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        return Ok(new { sentSuccessfully = true });
    }
}