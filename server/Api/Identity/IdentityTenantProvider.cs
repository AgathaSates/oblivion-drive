using System.Security.Claims;
using OblivionDrive.Domain.AuthenticationModule;

namespace OblivionDrive.Api.Identity;

public class IdentityTenantProvider(IHttpContextAccessor contextAccessor) : ITenantProvider
{
    public Guid? UserId
    {
        get
        {
            var claimId = contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);

            if (claimId is null)
                return null;

            return Guid.Parse(claimId.Value);
        }
    }

    public bool IsInRole(string role)
    {
        return contextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }
}
