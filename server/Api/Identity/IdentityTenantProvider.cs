using System.Security.Claims;
using OblivionDrive.Domain.AuthenticationModule;

namespace OblivionDrive.Api.Identity;

public class IdentityTenantProvider(IHttpContextAccessor contextAccessor) : ITenantProvider
{
    public Guid? UserId
    {
        get
        {
            var claim = contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return claim is null ? null : Guid.Parse(claim.Value);
        }
    }

    public Guid? CompanyId
    {
        get
        {
            var claim = contextAccessor.HttpContext?.User.FindFirst("tenant_company_id");
            return claim is null ? null : Guid.Parse(claim.Value);
        }
    }

    public UserType? UserType
    {
        get
        {
            var claim = contextAccessor.HttpContext?.User.FindFirst("user_type");
            return claim is null ? null : Enum.Parse<UserType>(claim.Value);
        }
    }

    public bool IsInRole(string role) =>
        contextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
}
