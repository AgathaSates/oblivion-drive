using Microsoft.AspNetCore.Identity;

namespace OblivionDrive.Domain.AuthenticationModule;
public class User : IdentityUser<Guid>
{
    public string FullName { get; set; }
    public Guid? CompanyId { get; set; }
    public User? CompanyUser { get; set; }

    public UserType UserType { get; set; }

    public User() 
    {
        Id = Guid.NewGuid();
        EmailConfirmed = true;
    }
}

public record AuthenticatedUser(
    Guid Id,
    string Name,
    string Email,
    UserType UserType
);