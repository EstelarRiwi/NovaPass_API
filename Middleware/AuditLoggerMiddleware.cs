using System.Security.Claims;
using NovaPass_API.Infrastructure.MongoDB;

namespace NovaPass_API.Middleware;

public class AuditLoggerMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> _auditMethods = ["POST", "PUT", "PATCH", "DELETE"];

    private static readonly HashSet<string> _excludedPaths =
    [
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
    ];

    public async Task InvokeAsync(HttpContext context, ILogService log)
    {
        var method = context.Request.Method.ToUpper();
        var path   = context.Request.Path.Value?.ToLower() ?? "";

        var shouldAudit = _auditMethods.Contains(method)
            && !_excludedPaths.Any(e => path.StartsWith(e));

        await next(context);

        if (shouldAudit && context.Response.StatusCode < 400)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role   = context.User.FindFirstValue("role");

            await log.LogSystemAsync("audit", new
            {
                method      = method,
                path        = path,
                status_code = context.Response.StatusCode,
                role        = role,
                ip          = context.Connection.RemoteIpAddress?.ToString(),
                timestamp   = DateTime.UtcNow,
            }, userId: userId);
        }
    }
}