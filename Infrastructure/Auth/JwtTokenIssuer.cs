using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Bookkeeping.Infrastructure.Auth;

public sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly IConfiguration _config;

    public JwtTokenIssuer(IConfiguration config) => _config = config;

    public string Issue(Guid userId, string email, IReadOnlyCollection<BusinessRoleAssignment> roles)
    {
        var section = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // One claim per business the user belongs to, carrying that business's role.
        foreach (var r in roles)
            claims.Add(new Claim(BookkeepingClaims.BusinessRole, $"{r.BusinessId}:{r.Role}"));

        var token = new JwtSecurityToken(
            issuer: section["Issuer"],
            audience: section["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
