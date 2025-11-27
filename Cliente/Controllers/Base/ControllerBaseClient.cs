using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Client.Controllers.Base
{
    public abstract class ControllerBaseClient<T> : ControllerBase where T : class
    {
        #region Campos

        private readonly ILogClientService _logClientService;
        private readonly ILogger<T> _logger;

        #endregion

        #region Construtor

        protected ControllerBaseClient(ILogClientService logClientService, ILogger<T> logger)
        {
            _logClientService = logClientService;
            _logger = logger;
        }

        #endregion

        #region Métodos de Logging

        [NonAction]
        protected async Task<BadRequestObjectResult> LogErro(Exception ex, string metodo, string? variaveis = null)
        {
            var controllerName = typeof(T).Name;

            _logger.LogError(ex, "[{Controller}] - {Metodo}: {Message}. Variáveis: {Variaveis}",
                controllerName, metodo, ex.Message, variaveis);

            await _logClientService.LogErroAsync(ex, metodo, controllerName, variaveis);

            return BadRequest(new
            {
                error = ex.Message,
                timestamp = DateTimeOffset.UtcNow,
                controller = controllerName,
                method = metodo
            });
        }

        [NonAction]
        protected IActionResult LogErroAsync(Exception ex, string acao, string? contexto = null)
        {
            var mensagemLog = contexto != null
                ? $"[{acao}] Erro: {ex.Message} | Contexto: {contexto}"
                : $"[{acao}] Erro: {ex.Message}";

            _logger.LogError(ex, mensagemLog);

            return Erro("Ocorreu um erro ao processar a solicitação", 500);
        }

        [NonAction]
        protected async Task RegistraAcaoAsync(string acao, string? dadoAntigo = null, string? dadoNovo = null, string? descricao = null)
        {
            await _logClientService.LogAcaoAsync(acao, dadoAntigo, dadoNovo, descricao);
        }

        [NonAction]
        protected void LogInfo(string mensagem, string metodo)
        {
            var controllerName = typeof(T).Name;
            _logger.LogInformation("[{Controller}] - {Metodo}: {Mensagem}", controllerName, metodo, mensagem);
        }

        [NonAction]
        protected async Task LogInfoAsync(string mensagem, string metodo)
        {
            var controllerName = typeof(T).Name;
            _logger.LogInformation("[{Controller}] - {Metodo}: {Mensagem}", controllerName, metodo, mensagem);

            await _logClientService.LogInfoAsync(mensagem, metodo, controllerName);
        }

        [NonAction]
        protected async Task LogRequisicaoAsync(string metodo, string endpoint, string acao, object? dadosEnviados = null, bool sucesso = true, string? descricao = null)
        {
            await _logClientService.LogRequisicaoAsync(metodo, endpoint, acao, dadosEnviados, sucesso, descricao);
        }

        #endregion

        #region Métodos Auxiliares

        [NonAction]
        protected string? ObterIdUsuario()
        {
            return User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                   User?.FindFirst("UsuarioId")?.Value ??
                   User?.FindFirst("sub")?.Value ??
                   HttpContext?.Items["UsuarioId"]?.ToString();
        }

        [NonAction]
        protected string? ObterUsuarioIdDoContexto()
        {
            return HttpContext.Items["UsuarioId"]?.ToString();
        }

        [NonAction]
        protected string? ObterIdEmpresa()
        {
            return User?.FindFirst("EmpresaId")?.Value ??
                   HttpContext?.Items["EmpresaId"]?.ToString();
        }

        [NonAction]
        protected string? ObterEmpresaIdDoContexto()
        {
            return HttpContext.Items["EmpresaId"]?.ToString();
        }

        [NonAction]
        protected string ObterIdUsuarioObrigatorio()
        {
            var usuarioId = ObterIdUsuario();
            if (string.IsNullOrEmpty(usuarioId))
                throw new UnauthorizedAccessException("Usuário não autenticado");
            return usuarioId;
        }

        [NonAction]
        protected string ObterIdEmpresaObrigatorio()
        {
            var empresaId = ObterIdEmpresa();
            if (string.IsNullOrEmpty(empresaId))
                throw new UnauthorizedAccessException("Empresa não identificada no contexto");
            return empresaId;
        }

        [NonAction]
        protected bool IsAdministrador()
        {
            return User?.FindFirst("FlgAdministrador")?.Value == "true" ||
                   User?.IsInRole("Administrador") == true;
        }

        [NonAction]
        protected bool VerificarPermissaoAdministrador()
        {
            var flgAdministrador = User.FindFirst("FlgAdministrador")?.Value;
            return flgAdministrador?.ToLower() == "true";
        }

        [NonAction]
        protected string ObterIpAddress()
        {
            try
            {
                var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                    return forwardedFor.Split(',')[0].Trim();

                var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                    return realIp;

                return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconhecido";
            }
            catch
            {
                return "Desconhecido";
            }
        }

        [NonAction]
        protected string ObterUserAgent()
        {
            return Request.Headers["User-Agent"].FirstOrDefault() ?? "Desconhecido";
        }

        [NonAction]
        protected string SerializarParaLog(object? obj)
        {
            try
            {
                if (obj == null) return "null";

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                return JsonSerializer.Serialize(obj, options);
            }
            catch
            {
                return obj?.ToString() ?? "null";
            }
        }

        #endregion

        #region Métodos de Resposta

        [NonAction]
        protected OkObjectResult Sucesso(object? data = null, string mensagem = "Operação realizada com sucesso")
        {
            return Ok(new
            {
                sucesso = true,
                mensagem = mensagem,
                data,
                timestamp = DateTimeOffset.UtcNow
            });
        }

        [NonAction]
        protected BadRequestObjectResult Erro(string mensagem, string? codigoErro = null)
        {
            return BadRequest(new
            {
                sucesso = false,
                mensagem = mensagem,
                errorCode = codigoErro,
                timestamp = DateTimeOffset.UtcNow
            });
        }

        [NonAction]
        protected IActionResult Erro(string mensagem, int statusCode)
        {
            var response = new
            {
                sucesso = false,
                mensagem = mensagem,
                timestamp = DateTime.UtcNow
            };
            return StatusCode(statusCode, response);
        }

        [NonAction]
        protected IActionResult ErroValidacao(Dictionary<string, string[]> erros)
        {
            var response = new
            {
                sucesso = false,
                mensagem = "Erro de validação",
                errors = erros,
                timestamp = DateTime.UtcNow
            };
            return BadRequest(response);
        }

        [NonAction]
        protected NotFoundObjectResult NaoEncontrado(string mensagem)
        {
            return NotFound(new
            {
                sucesso = false,
                mensagem = mensagem,
                timestamp = DateTimeOffset.UtcNow
            });
        }

        [NonAction]
        protected CreatedResult Criado(string uri, object? data, string mensagem = "Recurso criado com sucesso")
        {
            return Created(uri, new
            {
                sucesso = true,
                mensagem = mensagem,
                data,
                timestamp = DateTimeOffset.UtcNow
            });
        }

        #endregion
    }
}