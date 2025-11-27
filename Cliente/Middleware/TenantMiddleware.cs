using Client_Repository.Configuration.Contexto.Interface;
using System.Security.Claims;

namespace Client.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IContextoMultiTenantService contextoMultiTenant)
        {
            var path = context.Request.Path.Value?.ToLower();

            var isPublicEndpoint = path != null && (    
                path.Contains("/health") ||
                path.Contains("/swagger") ||
                path.Contains("/api/autenticacao") ||
                path.Contains("/api/webhookwaha") ||
                path.Contains("/api/n8n")); 

            if (isPublicEndpoint)
            {
                await _next(context);
                return;
            }

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                try
                {
                    var usuarioId = ExtrairClaim(context, "UsuarioId", "IdUsuario", ClaimTypes.NameIdentifier, "sub");
                    var empresaId = ExtrairClaim(context, "EmpresaId");

                    if (!string.IsNullOrEmpty(usuarioId) && !string.IsNullOrEmpty(empresaId))
                    {
                        context.Items["UsuarioId"] = usuarioId;
                        context.Items["EmpresaId"] = empresaId;

                        if (!contextoMultiTenant.TemContextoUsuario(usuarioId, empresaId))
                        {
                            _logger.LogInformation(
                                "Configurando contexto tenant para Usuario: {UsuarioId}, Empresa: {EmpresaId}",
                                usuarioId, empresaId);

                            await contextoMultiTenant.ConfigurarContextoUsuarioAsync(usuarioId, empresaId);
                        }

                        _logger.LogDebug(
                            "Request processado no contexto - Usuario: {UsuarioId}, Empresa: {EmpresaId}, Path: {Path}",
                            usuarioId, empresaId, path);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Claims incompletas - UsuarioId: {UsuarioId}, EmpresaId: {EmpresaId}",
                            usuarioId ?? "NULL", empresaId ?? "NULL");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning(ex, "Acesso negado ao configurar tenant");
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        sucesso = false,
                        mensagem = "Acesso negado: " + ex.Message,
                        timestamp = DateTimeOffset.UtcNow
                    });
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao configurar contexto tenant");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        sucesso = false,
                        mensagem = "Erro ao configurar contexto do tenant",
                        timestamp = DateTimeOffset.UtcNow
                    });
                    return;
                }
            }

            await _next(context);
        }

        private string? ExtrairClaim(HttpContext context, params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var claim = context.User?.FindFirst(claimType);
                if (claim != null && !string.IsNullOrEmpty(claim.Value))
                    return claim.Value;
            }
            return null;
        }
    }

    public static class TenantMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantMiddleware>();
        }
    }
}