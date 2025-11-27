using Admin_Service.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ADM.Controllers.Base
{
    /// <summary>
    /// Controlador base que fornece funcionalidades comuns para todos os controladores do sistema Admin.
    /// Inclui logging automático de requisições, erros e ações dos usuários.
    /// </summary>
    /// <typeparam name="T">Tipo do controlador que herda esta classe base.</typeparam>
    public abstract class ControllerBaseComplemento<T> : ControllerBase where T : class
    {
        #region Campos
        private readonly ILogADMService _logADMService;
        private readonly ILogger<T> _logger;
        #endregion

        #region Construtor

        protected ControllerBaseComplemento(ILogADMService logADMService, ILogger<T> logger)
        {
            _logADMService = logADMService;
            _logger = logger;
        }

        #endregion

        #region Métodos de Logging

        /// <summary>
        /// Registra um erro no sistema e retorna uma resposta BadRequest com a mensagem de erro.
        /// Este método não deve ser exposto como um endpoint de API.
        /// </summary>
        /// <param name="ex">Exceção que ocorreu.</param>
        /// <param name="metodo">Nome do método onde ocorreu o erro</param>
        /// <param name="variaveis">Variáveis relacionadas ao erro</param>
        /// <returns>BadRequestObjectResult com a mensagem de erro.</returns>
        [NonAction]
        protected async Task<BadRequestObjectResult> LogErroAsync(Exception ex, string metodo, string? variaveis = null)
        {
            var controllerName = typeof(T).Name;

            // Log no sistema de logs do .NET
            _logger.LogError(ex, "[{Controller}] - {Metodo}: {Message}. Variáveis: {Variaveis}",
                controllerName, metodo, ex.Message, variaveis);

            // Registra o erro no banco de dados Admin
            await _logADMService.LogErroAsync(ex, metodo, controllerName, variaveis);

            return BadRequest(new
            {
                error = ex.Message,
                timestamp = DateTimeOffset.UtcNow,
                controller = controllerName,
                method = metodo
            });
        }

        /// <summary>
        /// Registra uma ação ou alteração realizada no sistema.
        /// Este método não deve ser exposto como um endpoint de API.
        /// </summary>
        /// <param name="acao">Descrição da ação</param>
        /// <param name="dadoAntigo">Dados antes da alteração</param>
        /// <param name="dadoNovo">Dados após a alteração</param>
        /// <param name="descricao">Descrição adicional</param>
        [NonAction]
        protected async Task RegistraAcaoAsync(string acao, string? dadoAntigo = null, string? dadoNovo = null, string? descricao = null)
        {
            await _logADMService.LogAcaoAsync(acao, dadoAntigo, dadoNovo, descricao);
        }

        /// <summary>
        /// Registra informações no log do sistema.
        /// Este método não deve ser exposto como um endpoint de API.
        /// </summary>
        /// <param name="mensagem">Mensagem a ser registrada</param>
        /// <param name="metodo">Nome do método</param>
        [NonAction]
        protected void LogInfo(string mensagem, string metodo)
        {
            var controllerName = typeof(T).Name;
            _logger.LogInformation("[{Controller}] - {Metodo}: {Mensagem}", controllerName, metodo, mensagem);
        }

        /// <summary>
        /// Registra informações no log do sistema de forma assíncrona (salva no banco).
        /// </summary>
        /// <param name="mensagem">Mensagem a ser registrada</param>
        /// <param name="metodo">Nome do método</param>
        [NonAction]
        protected async Task LogInfoAsync(string mensagem, string metodo)
        {
            var controllerName = typeof(T).Name;
            _logger.LogInformation("[{Controller}] - {Metodo}: {Mensagem}", controllerName, metodo, mensagem);

            await _logADMService.LogInfoAsync(mensagem, metodo, controllerName);
        }

        /// <summary>
        /// Registra uma requisição completa com todos os dados
        /// </summary>
        /// <param name="metodo">Método HTTP</param>
        /// <param name="endpoint">Endpoint chamado</param>
        /// <param name="acao">Ação realizada</param>
        /// <param name="dadosEnviados">Dados enviados na requisição</param>
        /// <param name="sucesso">Se a operação foi bem-sucedida</param>
        /// <param name="descricao">Descrição adicional</param>
        [NonAction]
        protected async Task LogRequisicaoAsync(string metodo, string endpoint, string acao, object? dadosEnviados = null, bool sucesso = true, string? descricao = null)
        {
            await _logADMService.LogRequisicaoAsync(metodo, endpoint, acao, dadosEnviados, sucesso, descricao);
        }

        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Obtém o ID do usuário autenticado
        /// </summary>
        /// <returns>ID do usuário ou null se não autenticado</returns>
        [NonAction]
        protected string? ObterIdUsuario()
        {
            return User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                   User?.FindFirst("UsuarioId")?.Value ??
                   User?.FindFirst("sub")?.Value;
        }

        /// <summary>
        /// Obtém o ID da empresa do usuário autenticado
        /// </summary>
        /// <returns>ID da empresa ou null se não disponível</returns>
        [NonAction]
        protected string? ObterIdEmpresa()
        {
            return HttpContext?.Items["EmpresaId"]?.ToString();
        }

        /// <summary>
        /// Verifica se o usuário é administrador
        /// </summary>
        /// <returns>True se for administrador</returns>
        [NonAction]
        protected bool IsAdministrador()
        {
            return User?.FindFirst("FlgAdministrador")?.Value == "true" ||
                   User?.IsInRole("Administrador") == true;
        }

        /// <summary>
        /// Obtém o IP da requisição
        /// </summary>
        /// <returns>IP da requisição</returns>
        [NonAction]
        protected string ObterIpAddress()
        {
            try
            {
                // Verificar se há proxy (X-Forwarded-For)
                var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    return forwardedFor.Split(',')[0].Trim();
                }

                // Verificar se há proxy (X-Real-IP)
                var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                {
                    return realIp;
                }

                // IP direto
                return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconhecido";
            }
            catch
            {
                return "Desconhecido";
            }
        }

        /// <summary>
        /// Obtém o User Agent da requisição
        /// </summary>
        /// <returns>User Agent</returns>
        [NonAction]
        protected string ObterUserAgent()
        {
            return Request.Headers["User-Agent"].FirstOrDefault() ?? "Desconhecido";
        }

        /// <summary>
        /// Serializa um objeto para JSON (para logging)
        /// </summary>
        /// <param name="obj">Objeto a ser serializado</param>
        /// <returns>JSON string</returns>
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

        /// <summary>
        /// Cria uma resposta de sucesso padronizada
        /// </summary>
        /// <param name="data">Dados a serem retornados</param>
        /// <param name="mensagem">Mensagem de sucesso</param>
        /// <returns>OkObjectResult</returns>
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

        /// <summary>
        /// Cria uma resposta de erro padronizada
        /// </summary>
        /// <param name="mensagem">Mensagem de erro</param>
        /// <param name="codigoErro">Código do erro (opcional)</param>
        /// <returns>BadRequestObjectResult</returns>
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

        #endregion
    }
}
