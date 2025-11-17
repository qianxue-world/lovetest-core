using ActivationCodeApi.Services;

namespace ActivationCodeApi.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private const string AUTH_HEADER = "Authorization";

    public ApiKeyAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TokenService tokenService)
    {
        // Skip authentication for login endpoint
        if (context.Request.Path.StartsWithSegments("/api/admin/login"))
        {
            await _next(context);
            return;
        }

        // Check JWT token for admin endpoints
        if (context.Request.Path.StartsWithSegments("/api/admin"))
        {
            if (!context.Request.Headers.TryGetValue(AUTH_HEADER, out var authHeader))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Authorization token is missing");
                return;
            }

            var token = authHeader.ToString().Replace("Bearer ", "").Trim();
            
            var principal = tokenService.ValidateToken(token);
            if (principal == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid or expired token");
                return;
            }

            // Set the user principal for the request
            context.User = principal;
        }

        await _next(context);
    }
}
