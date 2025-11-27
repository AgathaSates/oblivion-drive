using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OblivionDrive.Api.Helpers;
using OblivionDrive.Api.Models.EmployeeModule;
using OblivionDrive.Application.EmployeeModule.Commands;
using OblivionDrive.Application.EmployeeModule.Querys;
using Swashbuckle.AspNetCore.Annotations;

namespace OblivionDrive.Api.Controllers;

[ApiController]
[Route("api/employee")]
[Authorize]
public class EmployeeController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Registrar um novo funcionário",
        Description = "Registra um novo funcionário no sistema. Acesso permitido apenas a usuários com o cargo 'Company'."
    )]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<RegisterEmployeeResponse>> Register(RegisterEmployeeRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<RegisterEmployeeCommand>(request);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        return Ok(result.Value);
    }

    [HttpPatch("{employeeId:guid}")]
    [SwaggerOperation(
    Summary = "Atualizar funcionário (empresa)",
    Description = "Atualiza os dados de um funcionário (nome, data de admissão e salário). Acesso permitido apenas a usuários com o cargo 'Company'."
    )]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<UpdateEmployeeByCompanyResponse>> UpdateByCompany(
        Guid employeeId, UpdateEmployeeByCompanyRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateEmployeeByCompanyCommand(
        employeeId,
        request.Name,
        request.HireDate,
        request.Salary
        );

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        return Ok(result.Value);
    }

    [HttpPatch("profile")]
    [SwaggerOperation(
    Summary = "Atualizar perfil do funcionário (próprio usuário)",
    Description = "Atualiza apenas o nome do funcionário logado. Acesso permitido apenas ao perfil 'Employee'."
    )]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<UpdateOwnEmployeeResponse>> UpdateOwnProfile(
    UpdateOwnEmployeeRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateOwnEmployeeProfileCommand(request.Name);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        return Ok(result.Value);
    }

    [HttpDelete("{employeeId:guid}")]
    [SwaggerOperation(
        Summary = "Excluir funcionário (empresa)",
        Description = "Exclui um funcionário da empresa. Acesso permitido apenas a usuários com o cargo 'Company'."
    )]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<DeleteEmployeeByCompanyResponse>> DeleteByCompany(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteEmployeeByCompanyCommand(employeeId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        var response = new DeleteEmployeeByCompanyResponse
        {
            DeletedSuccessfully = true,
            EmployeeId = employeeId
        };

        return Ok(response);
    }

    [HttpGet("{employeeId:guid}")]
    [SwaggerOperation(
        Summary = "Obter funcionário por Id (empresa)",
        Description = "Retorna os dados de um funcionário (nome, data de admissão e salário). Acesso permitido apenas a usuários com o cargo 'Company'."
    )]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<GetEmployeeByCompanyResponse>> GetByIdForCompany(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var query = new GetEmployeeByIdForCompanyQuery(employeeId);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        return Ok(result.Value);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Listar todos os funcionários da empresa",
        Description = "Retorna os funcionários cadastrados da empresa do usuário logado. Acesso permitido apenas a usuários com o cargo 'Company'."
    )]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<GetAllEmployeesForCompanyResponse>> GetAllForCompany(int? quantity,
        CancellationToken cancellationToken)
    {
        var query = new GetAllEmployeesForCompanyQuery(quantity);

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        GetAllEmployeesForCompanyResponse response =
        mapper.Map<GetAllEmployeesForCompanyResponse>(result.Value);

        return Ok(response);
    }
}