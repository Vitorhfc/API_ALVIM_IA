using System.Security.Claims;

namespace Client.Middleware
{
    public class ReloginMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ReloginMiddleware> _logger;

        public ReloginMiddleware(RequestDelegate next, ILogger<ReloginMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();

            var isPublicEndpoint = path != null && (
                path.Contains("/health") ||
                path.Contains("/swagger") ||
                path.Contains("/api/autenticacao"));

            if (isPublicEndpoint || context.User?.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            var proximoReloginClaim = context.User.FindFirst("ProximoRelogin")?.Value;

            if (!string.IsNullOrEmpty(proximoReloginClaim))
            {
                if (DateTime.TryParse(proximoReloginClaim, out var proximoRelogin))
                {
                    if (DateTime.Now >= proximoRelogin)
                    {
                        _logger.LogWarning("Relogin obrigatório detectado para usuário");

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.Headers.Append("X-Relogin-Required", "true");

                        await context.Response.WriteAsJsonAsync(new
                        {
                            sucesso = false,
                            mensagem = "Relogin obrigatório. Por favor, faça login novamente.",
                            reloginRequired = true,
                            timestamp = DateTime.UtcNow
                        });
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    public static class ReloginMiddlewareExtensions
    {
        public static IApplicationBuilder UseReloginMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ReloginMiddleware>();
        }
    }
}