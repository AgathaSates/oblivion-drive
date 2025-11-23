using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace OblivionDrive.Api.Helpers;

public static class ResultErrorHandler
{
    private const string ErrorTypeMetadataKey = "ErrorType";

    public static ActionResult ToActionResult(this IResultBase result)
    {
        List<string> errorMessages = result.Errors
            .SelectMany(e => e.Reasons.OfType<IError>())
            .Select(e => e.Message)
            .ToList();

        bool HasErrorType(string type) =>
            result.Errors.Any(e =>
                e.HasMetadata(ErrorTypeMetadataKey,
                    value => string.Equals(value?.ToString(), type, StringComparison.OrdinalIgnoreCase)));

        // 401 – não autorizado (token inválido)
        if (HasErrorType("Unauthorized"))
        {
            return new UnauthorizedObjectResult(errorMessages);
        }

        // 400 – validação / erros de regra
        if (HasErrorType("InvalidRequest") ||
            HasErrorType("BadRequest") ||
            HasErrorType("DuplicateRecord"))
        {
            return new BadRequestObjectResult(errorMessages);
        }

        // 404 – não encontrado
        if (HasErrorType("RecordNotFound"))
        {
            return new NotFoundObjectResult(errorMessages);
        }

        // 409 – conflito (exclusão bloqueada etc.)
        if (HasErrorType("DeletionBlocked"))
        {
            return new ConflictObjectResult(errorMessages);
        }

        // 500 – qualquer coisa não mapeada explicitamente
        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
    }
}