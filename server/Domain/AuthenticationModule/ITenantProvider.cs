namespace OblivionDrive.Domain.AuthenticationModule;
public interface ITenantProvider
{
    Guid? UserId { get; }
    bool IsInRole(string role);

}