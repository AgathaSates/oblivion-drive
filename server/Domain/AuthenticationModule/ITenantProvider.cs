namespace OblivionDrive.Domain.AuthenticationModule;
public interface ITenantProvider
{
    Guid? UserId { get; }
    Guid? CompanyId { get; }
    UserType? UserType { get; }
    bool IsInRole(string role);

}