using AutoMapper.Internal.Mappers;
using Client_Repository.Configuration.Contexto.Interface;
using Client_Repository.Repositorio.Interface;
using Client_Repository.RepositorioGenerico.Interface;
using Client_Service.Service.Interface;
using Client_Service.ServiceGenerico;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using Shared.Classes.Entidades.Client;
using Shared.Utils;
using System.Text.Json;

namespace Client_Service.Service
{
    public class LogClientService : ServiceGenerico<LogClient>, ILogClientService
    {
        #region Fields
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IContextoMultiTenantService _contextoMultiTenant;
        private readonly ILogClientRepositorio _repositorio;

        // ObjectIds especiais para valores do sistema
        private static readonly string OBJECTID_SISTEMA = "000000000000000000000001";  // Representa usuário/empresa "Sistema"
        private static readonly string OBJECTID_WEBHOOK = "000000000000000000000002";  // Representa webhooks anônimos
        private static readonly string OBJECTID_DESCONHECIDO = "000000000000000000000003";  // Representa valores desconhecidos
        #endregion

        #region Constructor
        public LogClientService(
            ILogClientRepositorio repositorio,
            IHttpContextAccessor httpContextAccessor,
            IContextoMultiTenantService contextoMultiTenant)
            : base(repositorio)
        {
            _httpContextAccessor = httpContextAccessor;
            _contextoMultiTenant = contextoMultiTenant;
            _repositorio = repositorio;
        }
        #endregion

        #region Public Methods
        public async Task LogErroAsync(Exception ex, string metodo, string controller, string? variaveis = null)
        {
            try
            {
                var log = CriarLogBase("Erro", controller, metodo);
                log.Mensagem = ex.Message;
                log.Descricao = $"{ex.StackTrace}{(ex.InnerException != null ? $"\nInner Exception: {ex.InnerException.Message}" : "")}";
                log.DadoAntigo = variaveis;
                log.Sucesso = false;

                await _repositorio.AdicionarAsync(log);
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Falha ao registrar log de erro: {logEx.Message}");
            }
        }

        public async Task LogAcaoAsync(string acao, string? dadoAntigo = null, string? dadoNovo = null, string? descricao = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var controller = httpContext?.Request.RouteValues["controller"]?.ToString() ?? "Sistema";
                var metodo = httpContext?.Request.RouteValues["action"]?.ToString() ?? acao;

                var log = CriarLogBase("Acao", controller, metodo);
                log.Acao = acao;
                log.DadoAntigo = dadoAntigo;
                log.DadoNovo = dadoNovo;
                log.Descricao = descricao ?? $"Ação executada: {acao}";
                log.Sucesso = true;

                await _repositorio.AdicionarAsync(log);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Falha ao registrar log de ação: {ex.Message}");
            }
        }

        public async Task LogInfoAsync(string mensagem, string metodo, string controller)
        {
            try
            {
                var log = CriarLogBase("Info", controller, metodo);
                log.Mensagem = mensagem;
                log.Descricao = $"Informação registrada em {controller}.{metodo}";
                log.Sucesso = true;

                await _repositorio.AdicionarAsync(log);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Falha ao registrar log de informação: {ex.Message}");
            }
        }

        public async Task LogRequisicaoAsync(string metodo, string endpoint, string acao, object? dadosEnviados = null, bool sucesso = true, string? descricao = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var controller = httpContext?.Request.RouteValues["controller"]?.ToString() ?? ExtractControllerFromEndpoint(endpoint);

                var log = CriarLogBase("Requisicao", controller, metodo);
                log.Acao = acao;
                log.Endpoint = endpoint;
                log.MetodoHttp = httpContext?.Request.Method ?? metodo;
                log.Sucesso = sucesso;
                log.Descricao = descricao ?? $"Requisição {log.MetodoHttp} para {endpoint}";
                log.Mensagem = sucesso ? "Requisição processada com sucesso" : "Falha no processamento da requisição";

                if (dadosEnviados != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    log.DadoNovo = JsonSerializer.Serialize(dadosEnviados, options);
                }

                await _repositorio.AdicionarAsync(log);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Falha ao registrar log de requisição: {ex.Message}");
            }
        }

        public async Task<IEnumerable<LogClient>> BuscarLogsPorPeriodoAsync(DateTime dataInicio, DateTime dataFim)
        {
            try
            {
                return await _repositorio.BuscarPorFiltroAsync(l =>
                    l.DtaCadastro >= dataInicio &&
                    l.DtaCadastro <= dataFim);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs por período: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<LogClient>> BuscarLogsPorTipoAsync(string tipo)
        {
            try
            {
                return await _repositorio.BuscarPorFiltroAsync(l => l.Tipo == EnumHelper.StringToEnum<TipoLog>(tipo));
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs por tipo: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<LogClient>> BuscarLogsPorUsuarioAsync(string usuarioId)
        {
            try
            {
                return await _repositorio.BuscarPorFiltroAsync(l => l.UsuarioId == usuarioId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs por usuário: {ex.Message}", ex);
            }
        }
        #endregion

        #region Private Methods
        private LogClient CriarLogBase(string tipo, string controller, string metodo)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var log = new LogClient
            {
                Tipo = EnumHelper.StringToEnum<TipoLog>(tipo),
                Controller = controller,
                Metodo = metodo,
                Endpoint = httpContext?.Request.Path.Value ?? "",
                MetodoHttp = httpContext?.Request.Method ?? "",
                DtaCadastro = DateTime.UtcNow,
                UsuarioId = NormalizarUsuarioId(ObterUsuarioId()),
                EmpresaId = NormalizarEmpresaId(ObterEmpresaId()),
                IpAddress = ObterIpAddress(),
                UserAgent = ObterUserAgent()
            };

            return log;
        }

        private string? ObterUsuarioId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            return httpContext.Items["UsuarioId"]?.ToString() ??
                   httpContext.User?.FindFirst("UsuarioId")?.Value ??
                   httpContext.User?.FindFirst("sub")?.Value ??
                   _contextoMultiTenant.ObterIdUsuarioAtual();
        }

        private string? ObterEmpresaId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            return httpContext.Items["EmpresaId"]?.ToString() ??
                   httpContext.User?.FindFirst("EmpresaId")?.Value ??
                   httpContext.User?.FindFirst("tenant")?.Value ??
                   _contextoMultiTenant.ObterIdEmpresaAtual();
        }

        private string ObterIpAddress()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                    return "Desconhecido";

                var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                    return forwardedFor.Split(',')[0].Trim();

                var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                    return realIp;

                return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconhecido";
            }
            catch
            {
                return "Desconhecido";
            }
        }

        private string ObterUserAgent()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                    return "Desconhecido";

                return httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Desconhecido";
            }
            catch
            {
                return "Desconhecido";
            }
        }

        private string ExtractControllerFromEndpoint(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return "Desconhecido";

            var parts = endpoint.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : "Desconhecido";
        }

        /// <summary>
        /// Normaliza o UsuarioId para garantir que seja um ObjectId válido
        /// </summary>
        private string NormalizarUsuarioId(string? usuarioId)
        {
            if (string.IsNullOrWhiteSpace(usuarioId))
                return OBJECTID_SISTEMA;

            // Se já é um ObjectId válido, retorna
            if (ObjectId.TryParse(usuarioId, out _))
                return usuarioId;

            // Casos especiais
            if (usuarioId.Equals("Sistema", StringComparison.OrdinalIgnoreCase))
                return OBJECTID_SISTEMA;

            if (usuarioId.StartsWith("webhook_", StringComparison.OrdinalIgnoreCase))
                return OBJECTID_WEBHOOK;

            // Se não conseguir converter, retorna desconhecido
            return OBJECTID_DESCONHECIDO;
        }

        /// <summary>
        /// Normaliza o EmpresaId para garantir que seja um ObjectId válido
        /// </summary>
        private string NormalizarEmpresaId(string? empresaId)
        {
            if (string.IsNullOrWhiteSpace(empresaId))
                return OBJECTID_SISTEMA;

            // Se já é um ObjectId válido, retorna
            if (ObjectId.TryParse(empresaId, out _))
                return empresaId;

            // Casos especiais
            if (empresaId.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
                empresaId.Equals("Sistema", StringComparison.OrdinalIgnoreCase))
                return OBJECTID_SISTEMA;

            // Se não conseguir converter, retorna desconhecido
            return OBJECTID_DESCONHECIDO;
        }
        #endregion


        public async Task RegistrarWebhook(
            string idLog,
            string origem,
            string evento,
            string payloadJson)
        {
            try
            {
                var log = new LogClient
                {
                    Tipo = TipoLog.Webhook,
                    Origem = origem,
                    Acao = evento,
                    Mensagem = $"Webhook recebido: {evento}",
                    DadoNovo = payloadJson,
                    Sucesso = true,
                    NivelSeveridade = NivelSeveridade.Info,
                    DtaCadastro = DateTime.UtcNow,
                    UsuarioId = NormalizarUsuarioId(ObterUsuarioId()),
                    EmpresaId = NormalizarEmpresaId(ObterEmpresaId()),
                    IpAddress = ObterIpAddress(),
                    UserAgent = ObterUserAgent()
                };

                await _repositorio.AdicionarAsync(log);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task RegistrarErro(
            string metodo,
            string mensagem,
            string payload,
            string? stackTrace)
        {
            try
            {
                var log = new LogClient
                {
                    Tipo = TipoLog.Erro,
                    Metodo = metodo,
                    Mensagem = mensagem,
                    DadoNovo = payload,
                    Descricao = stackTrace,
                    Sucesso = false,
                    NivelSeveridade = NivelSeveridade.Error,
                    DtaCadastro = DateTime.UtcNow,
                    UsuarioId = NormalizarUsuarioId(ObterUsuarioId()),
                    EmpresaId = NormalizarEmpresaId(ObterEmpresaId()),
                    IpAddress = ObterIpAddress(),
                    UserAgent = ObterUserAgent()
                };

                await _repositorio.AdicionarAsync(log);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task RegistrarInfo(
            string origem,
            string mensagem,
            string? dadosAdicionais = null)
        {
            try
            {
                var log = new LogClient
                {
                    Tipo = TipoLog.Sistema,
                    Origem = origem,
                    Mensagem = mensagem,
                    DadoNovo = dadosAdicionais,
                    Sucesso = true,
                    NivelSeveridade = NivelSeveridade.Info,
                    DtaCadastro = DateTime.UtcNow,
                    UsuarioId = NormalizarUsuarioId(ObterUsuarioId()),
                    EmpresaId = NormalizarEmpresaId(ObterEmpresaId()),
                    IpAddress = ObterIpAddress(),
                    UserAgent = ObterUserAgent()
                };

                await _repositorio.AdicionarAsync(log);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}