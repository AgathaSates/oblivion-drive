using FluentResults;

namespace OblivionDrive.Application.Shared;
public abstract class ErrorResults
{
    public static Error InvalidRequestError(string error)
    {
        return new Error("Requisição inválida")
            .CausedBy(error)
            .WithMetadata("ErrorType", "InvalidRequest");
    }

    public static Error InvalidRequestError(IEnumerable<string> errors)
    {
        return new Error("Requisição inválida")
            .CausedBy(errors)
            .WithMetadata("ErrorType", "InvalidRequest");
    }

    public static Error DuplicateRecordError(string errorMessage)
    {
        return new Error("Registro duplicado")
            .CausedBy(errorMessage)
            .WithMetadata("ErrorType", "DuplicateRecord");
    }

    public static Error RecordNotFoundError(Guid id)
    {
        return new Error("Registro não encontrado")
            .CausedBy("Não foi possível obter o registro com o ID: " + id)
            .WithMetadata("ErrorType", "RecordNotFound");
    }

    public static Error RecordNotFoundError()
    {
        return new Error("Registro não encontrado")
            .CausedBy("Não foi possível obter o registro com nenhuma informação passada")
            .WithMetadata("ErrorType", "RecordNotFound");
    }

    public static Error RecordNotFoundError(string record)
    {
        return new Error("Registro não encontrado")
            .CausedBy("Não foi possível obter o registro: " + record)
            .WithMetadata("ErrorType", "RecordNotFound");
    }

    public static Error IncorrectCredentialsError()
    {
        return new Error("Credenciais incorretas")
            .CausedBy("O nome de usuário ou a senha estão incorretos")
            .WithMetadata("ErrorType", "BadRequest");
    }

    public static Error UserNotFoundError(string username)
    {
        return new Error("Usuário não encontrado")
            .CausedBy($"O usuário com o nome '{username}' não foi encontrado")
            .WithMetadata("ErrorType", "BadRequest");
    }

    public static Error UnauthorizedError(string message)
    {
        return new Error("Não autorizado")
            .CausedBy(message)
            .WithMetadata("ErrorType", "Unauthorized");
    }

    public static Error DeletionBlockedError(string errorMessage)
    {
        return new Error("Exclusão bloqueada")
            .CausedBy(errorMessage)
            .WithMetadata("ErrorType", "DeletionBlocked");
    }

    public static Error InternalExceptionError(Exception ex)
    {
        return new Error("Ocorreu um erro interno do servidor")
            .CausedBy(ex)
            .WithMetadata("ErrorType", "InternalException");
    }
}