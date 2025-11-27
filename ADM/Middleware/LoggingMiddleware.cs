using Admin_Service.Service.Interface;
using System.Text;

namespace ADM.Middleware
{
    /// <summary>
    /// Middleware para capturar automaticamente dados das requisições e registrá-las no log
    /// </summary>
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ILogADMService logADMService)
        {
            var startTime = DateTime.UtcNow;
            var requestBody = await CapturarCorpoRequisicaoAsync(context.Request);
            var responseBody = await CapturarCorpoRespostaAsync(context);

            try
            {
                await _next(context);
            }
            finally
            {
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                await RegistrarLogRequisicaoAsync(context, logADMService, requestBody, responseBody, duration);
            }
        }

        private async Task<string> CapturarCorpoRequisicaoAsync(HttpRequest request)
        {
            try
            {
                if (request.ContentLength > 0 && request.ContentType?.Contains("application/json") == true)
                {
                    request.EnableBuffering();

                    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    request.Body.Position = 0;

                    // Limitar tamanho do log (primeiros 1000 caracteres)
                    return body.Length > 1000 ? body.Substring(0, 1000) + "..." : body;
                }
                return string.Empty;
            }
            catch
            {
                return "Erro ao capturar corpo da requisição";
            }
        }

        private async Task<string> CapturarCorpoRespostaAsync(HttpContext context)
        {
            try
            {
                var originalBodyStream = context.Response.Body;

                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                await _next(context);

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);

                context.Response.Body = originalBodyStream;

                // Limitar tamanho do log (primeiros 500 caracteres)
                return responseBody.Length > 500 ? responseBody.Substring(0, 500) + "..." : responseBody;
            }
            catch
            {
                return "Erro ao capturar corpo da resposta";
            }
        }

        private async Task RegistrarLogRequisicaoAsync(HttpContext context, ILogADMService logADMService, string requestBody, string responseBody, TimeSpan duration)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                // Não registrar logs para endpoints de saúde, swagger, etc.
                if (DeveIgnorarEndpoint(request.Path))
                    return;

                var metodo = request.Method;
                var endpoint = $"{request.Path}{request.QueryString}";
                var acao = $"Requisição {metodo} para {endpoint}";

                var sucesso = response.StatusCode >= 200 && response.StatusCode < 400;
                var descricao = $"Status: {response.StatusCode} | Duração: {duration.TotalMilliseconds}ms";

                var dadosEnviados = new
                {
                    requestBody = requestBody,
                    responseBody = responseBody,
                    headers = ObterHeadersRelevantes(request),
                    queryParams = ObterQueryParams(request)
                };

                await logADMService.LogRequisicaoAsync(metodo, endpoint, acao, dadosEnviados, sucesso, descricao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar log da requisição");
            }
        }

        private bool DeveIgnorarEndpoint(PathString path)
        {
            var caminhosIgnorados = new[]
            {
                "/health",
                "/swagger",
                "/favicon.ico",
                "/robots.txt",
                "/_framework",
                "/css",
                "/js",
                "/images"
            };

            return caminhosIgnorados.Any(caminho => path.StartsWithSegments(caminho));
        }

        private Dictionary<string, string> ObterHeadersRelevantes(HttpRequest request)
        {
            var headers = new Dictionary<string, string>();

            var headersRelevantes = new[]
            {
                "Authorization",
                "Content-Type",
                "User-Agent",
                "X-Forwarded-For",
                "X-Real-IP",
                "Referer",
                "Origin"
            };

            foreach (var header in headersRelevantes)
            {
                if (request.Headers.TryGetValue(header, out var values))
                {
                    headers[header] = values.ToString();
                }
            }

            return headers;
        }

        private Dictionary<string, string> ObterQueryParams(HttpRequest request)
        {
            var queryParams = new Dictionary<string, string>();

            foreach (var param in request.Query)
            {
                queryParams[param.Key] = param.Value.ToString();
            }

            return queryParams;
        }
    }

    /// <summary>
    /// Extensão para registrar o middleware
    /// </summary>
    public static class LoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseLoggingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LoggingMiddleware>();
        }
    }
}
