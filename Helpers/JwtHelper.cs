using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NovaPass_API.Models;

namespace NovaPass_API.Helpers;

public class JwtHelper(IConfiguration config)
{
    public string GenerateToken(User user)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? config["Jwt:Secret"]!;
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? config["Jwt:Issuer"]!;
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? config["Jwt:Audience"]!;
        var expiryHours = int.TryParse(config["Jwt:ExpiryHours"], out var h) ? h : 8;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Rol ?? "customer"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        
        foreach (var permission in user.Permisos ?? [])
            claims.Add(new Claim("permissions", permission));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
