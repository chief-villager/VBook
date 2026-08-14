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

    // Access tokens are deliberately short-lived: refresh-token rotation mints a new
    // one on demand, so a leaked access token is only useful for this window.
    private const int DefaultAccessTokenMinutes = 15;

    public IssuedAccessToken Issue(Guid userId, string email, IReadOnlyCollection<BusinessRoleAssignment> roles)
    {
        var section = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(
            section.GetValue<int?>("AccessTokenMinutes") ?? DefaultAccessTokenMinutes);

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
            expires: expiresAt,
            signingCredentials: credentials);

        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
