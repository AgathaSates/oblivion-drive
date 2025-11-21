using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OblivionDrive.Domain.AuthenticationModule;

namespace OblivionDrive.Application.AuthenticationModule;
public class AccessTokenProvider : ITokenProvider
{
    private readonly string validAudience;
    private readonly string jwtSigningKey;
    private readonly DateTime jwtExpiration;

    public AccessTokenProvider(IConfiguration config)
    {
        if (string.IsNullOrEmpty(config["JWT_GENERATION_KEY"]))
            throw new ArgumentException("Cifra de geração de tokens não configurada");

        jwtSigningKey = config["JWT_GENERATION_KEY"]!;

        if (string.IsNullOrEmpty(config["JWT_AUDIENCE_DOMAIN"]))
            throw new ArgumentException("Audiência válida para transmissão de tokens não configurada");

        validAudience = config["JWT_AUDIENCE_DOMAIN"]!;

        jwtExpiration = DateTime.UtcNow.AddMinutes(60);
    }

    public AccessToken CreateAcessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var chaveEmBytes = Encoding.ASCII.GetBytes(jwtSigningKey!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = "oblivion-drive-api",
            Audience = validAudience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!)
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(chaveEmBytes),
                SecurityAlgorithms.HmacSha256Signature
            ),
            Expires = jwtExpiration
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        var tokenString = tokenHandler.WriteToken(token);

        return new AccessToken(
            tokenString,
            jwtExpiration,
             new AuthenticatedUser(
                user.Id,
                user.FullName ?? string.Empty,
                user.Email ?? string.Empty,
                user.UserType
            )
        );
    }
}
