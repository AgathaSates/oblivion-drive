namespace OblivionDrive.Domain.AuthenticationModule;
public interface ITokenProvider
{
    AccessToken CreateAcessToken(User user);
}

public record AccessToken(
    string key,
    DateTime expiration,
    AuthenticatedUser authenticatedUser
);