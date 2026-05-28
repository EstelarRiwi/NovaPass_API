using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NovaPass_API.Data;

namespace NovaPass_API.Middleware;

public class JwtValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IConfiguration config, TicketEventsDbContext db)
    {
        var token = context.Request.Headers.Authorization
            .FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            try
            {
                var secret   = Environment.GetEnvironmentVariable("JWT_SECRET") ?? config["Jwt:Secret"]!;
                var issuer   = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? config["Jwt:Issuer"]!;
                var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? config["Jwt:Audience"]!;

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = key,
                    ValidateIssuer           = true,
                    ValidIssuer              = issuer,
                    ValidateAudience         = true,
                    ValidAudience            = audience,
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero
                }, out _);

                var jti = principal.FindFirst("jti")?.Value;
                if (jti != null && await db.TokenBlacklists.AnyAsync(t => t.Jti == jti))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "Token invalidado" });
                    return;
                }

                context.User = principal;
            }
            catch
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Token inválido o expirado" });
                return;
            }
        }

        await next(context);
    }
}