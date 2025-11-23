using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OblivionDrive.Application.AuthenticationModule;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Api.Identity;

public static class IdentityConfig
{
    public static void AddIdentityProviderConfig
        (this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITenantProvider, IdentityTenantProvider>();
        services.AddScoped<ITokenProvider, AccessTokenProvider>();

        services.AddIdentity<User, Role>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<OblivionDriveDbContext>()
        .AddDefaultTokenProviders();

        services.AddJwtAuthentication(configuration);
    }

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSigningKey = configuration["JWT_GENERATION_KEY"]
            ?? throw new ArgumentException("Não foi possível obter a chave de assinatura de tokens.");

        var validAudience = configuration["JWT_AUDIENCE_DOMAIN"]
            ?? throw new ArgumentException("Não foi possível obter o domínio da audiência dos tokens.");

        var bytesKey = Encoding.ASCII.GetBytes(jwtSigningKey);

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = true;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(bytesKey),
                ValidAudience = validAudience,
                ValidIssuer = "oblivion-drive-api",
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateLifetime = true,
            };
        });
    }
}