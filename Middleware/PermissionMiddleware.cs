namespace NovaPass_API.Middleware;

public class PermissionMiddleware(RequestDelegate next)
{
    private static readonly Dictionary<string, string> _routePermissions = new()
    {
        { "/api/tickets/sell",     "taquilla" },
        { "/api/tickets/validate", "acceso"   },
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        var requiredPermission = _routePermissions
            .FirstOrDefault(r => path.StartsWith(r.Key)).Value;

        if (requiredPermission != null)
        {
            var user = context.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "No autenticado" });
                return;
            }

            var permissions = user.FindAll("permissions").Select(c => c.Value).ToList();

            if (!permissions.Contains(requiredPermission))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { message = $"Requiere permiso: {requiredPermission}" });
                return;
            }
        }

        await next(context);
    }
}