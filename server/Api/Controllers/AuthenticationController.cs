using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Application.AuthenticationModule.Commands;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using Swashbuckle.AspNetCore.Annotations;
using OblivionDrive.Api.Helpers;

namespace OblivionDrive.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Registrar usuário",
        Description = "Registra um novo usuário no sistema.",
        Tags = new[] { "Autenticação" }
    )]
    public async Task<ActionResult<AccessToken>> Register(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
        request.UserName,
        request.Email,
        request.Password);

        var result = await mediator.Send(command);

        if (result.IsFailed)
            return result.ToActionResult();

        return Ok(result.Value);
    }

    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Logar usuário",
        Description = "Autentica um usuário existente no sistema.",
        Tags = new[] { "Autenticação" }
    )]
    public async Task<ActionResult<AccessToken>> Login(LoginUserRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<LoginUserCommand>(request);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToActionResult();

        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [SwaggerOperation(
        Summary = "Sair",
        Description = "Encerra a sessão do usuário autenticado.",
        Tags = new[] { "Autenticação" }
    )]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var result = await mediator.Send(new LogoutUserCommand());

        if (result.IsFailed)
            return result.ToActionResult();

        return NoContent();
    }
}
