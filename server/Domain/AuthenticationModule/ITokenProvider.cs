namespace OblivionDrive.Domain.AuthenticationModule;
public interface ITokenProvider
{
    IAccessToken CreateAcessToken(User user);
}