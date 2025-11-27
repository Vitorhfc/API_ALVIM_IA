using Admin_Repository.Repositorio.Interface;
using Admin_Service.Service.Interface;
using Admin_Service.ServiceGenerico;
using Microsoft.AspNetCore.Http;
using Shared.Classes.Entidades.ADM;
using System.Security.Claims;
using System.Text.Json;

namespace Admin_Service.Service
{
    /// <summary>
    /// Serviço específico para operações com a entidade LogADM
    /// </summary>
    public class LogADMService : ServiceGenerico<LogADM>, ILogADMService
    {
        private readonly ILogADMRepository _logADMRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogADMService(ILogADMRepository logADMRepository, IHttpContextAccessor httpContextAccessor) 
            : base(logADMRepository)
        {
            _logADMRepository = logADMRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Métodos Específicos

        public async Task<IEnumerable<LogADM>> BuscarPorUsuarioAsync(string usuarioId, int page = 1, int pageSize = 50)
        {
            try
            {
                return await _logADMRepository.BuscarPorUsuarioAsync(usuarioId, page, pageSize);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao buscar logs por usuário: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<LogADM>> BuscarPorEmpresaAsync(string empresaId, int page = 1, int pageSize = 50)
        {
            try
            {
                return await _logADMRepository.BuscarPorEmpresaAsync(empresaId, page, pageSize);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao buscar logs por empresa: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<LogADM>> BuscarPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, int page = 1, int pageSize = 50)
        {
            try
            {
                return await _logADMRepository.BuscarPorPeriodoAsync(dataInicio, dataFim, page, pageSize);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao buscar logs por período: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<LogADM>> BuscarPorAcaoAsync(string acao, int page = 1, int pageSize = 50)
        {
            try
            {
                return await _logADMRepository.BuscarPorAcaoAsync(acao, page, pageSize);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao buscar logs por ação: {ex.Message}", ex);
            }
        }

        public async Task<int> LimparLogsAntigosAsync(int dias = 90)
        {
            try
            {
                return await _logADMRepository.LimparLogsAntigosAsync(dias);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao limpar logs antigos: {ex.Message}", ex);
            }
        }

        #endregion

        #region Métodos de Logging

        /// <summary>
        /// Registra um log de requisição completa
        /// </summary>
        /// <param name="metodo">Método HTTP</param>
        /// <param name="endpoint">Endpoint chamado</param>
        /// <param name="acao">Ação realizada</param>
        /// <param name="dadosEnviados">Dados enviados na requisição</param>
        /// <param name="sucesso">Se a operação foi bem-sucedida</param>
        /// <param name="descricao">Descrição adicional</param>
        public async Task<LogADM> LogRequisicaoAsync(string metodo, string endpoint, string acao, object? dadosEnviados = null, bool sucesso = true, string? descricao = null)
        {
            try
            {
                var log = new LogADM
                {
                    UsuarioId = ObterIdUsuarioAtual(),
                    EmpresaId = ObterIdEmpresaAtual(),
                    Acao = $"{metodo} {endpoint} - {acao}",
                    Sucesso = sucesso,
                    Descricao = descricao ?? "Requisição processada",
                    IpAddress = ObterIpAddress(),
                    UserAgent = ObterUserAgent(),
                    DtaCadastro = DateTime.Now
                };

                return await AdicionarAsync(log);
            }
            catch (Exception ex)
            {
                // Log silencioso para não criar loop de erros
                Console.WriteLine($"Erro ao registrar log: {ex.Message}");
                return new LogADM(); // Retorna log vazio em caso de erro
            }
        }

        /// <summary>
        /// Registra um erro no sistema
        /// </summary>
        /// <param name="ex">Exceção ocorrida</param>
        /// <param name="metodo">Nome do método</param>
        /// <param name="controller">Nome do controller</param>
        /// <param name="variaveis">Variáveis relacionadas ao erro</param>
        public async Task<LogADM> LogErroAsync(Exception ex, string metodo, string controller, string? variaveis = null)
        {
            try
            {
                var log = new LogADM
                {
                    UsuarioId = ObterIdUsuarioAtual(),
                    EmpresaId = ObterIdEmpresaAtual(),
                    Acao = $"ERRO - {controller}.{metodo}",
                    Sucesso = false,
                    Descricao = $"Exceção: {ex.Message}. StackTrace: {ex.StackTrace}. Variáveis: {variaveis}",
                    IpAddress = ObterIpAddress(),
                    UserAgent = ObterUserAgent(),
                    DtaCadastro = DateTime.Now
                };

                return await AdicionarAsync(log);
            }
            catch
            {
                // Log silencioso para não criar loop de erros
                return new LogADM();
            }
        }

        /// <summary>
        /// Registra uma ação do usuário
        /// </summary>
        /// <param name="acao">Ação realizada</param>
        /// <param name="dadoAntigo">Dados antes da alteração</param>
        /// <param name="dadoNovo">Dados após a alteração</param>
        /// <param name="descricao">Descrição adicional</param>
        public async Task<LogADM> LogAcaoAsync(string acao, string? dadoAntigo = null, string? dadoNovo = null, string? descricao = null)
        {
            try
            {
                var descricaoCompleta = descricao ?? acao;
                
                if (!string.IsNullOrEmpty(dadoAntigo) || !string.IsNullOrEmpty(dadoNovo))
                {
                    descricaoCompleta += $" | Antes: {dadoAntigo} | Depois: {dadoNovo}";
                }

                var log = new LogADM
                {
                    UsuarioId = ObterIdUsuarioAtual(),
                    EmpresaId = ObterIdEmpresaAtual(),
                    Acao = acao,
                    Sucesso = true,
                    Descricao = descricaoCompleta,
                    IpAddress = ObterIpAddress(),
                    UserAgent = ObterUserAgent(),
                    DtaCadastro = DateTime.Now
                };

                return await AdicionarAsync(log);
            }
            catch
            {
                return new LogADM();
            }
        }

        /// <summary>
        /// Registra informações gerais
        /// </summary>
        /// <param name="mensagem">Mensagem a ser registrada</param>
        /// <param name="metodo">Nome do método</param>
        /// <param name="controller">Nome do controller</param>
        public async Task<LogADM> LogInfoAsync(string mensagem, string metodo, string controller)
        {
            try
            {
                var log = new LogADM
                {
                    UsuarioId = ObterIdUsuarioAtual(),
                    EmpresaId = ObterIdEmpresaAtual(),
                    Acao = $"INFO - {controller}.{metodo}",
                    Sucesso = true,
                    Descricao = mensagem,
                    IpAddress = ObterIpAddress(),
                    UserAgent = ObterUserAgent(),
                    DtaCadastro = DateTime.Now
                };

                return await AdicionarAsync(log);
            }
            catch
            {
                return new LogADM();
            }
        }

        #endregion

        #region Métodos Auxiliares

        private string? ObterIdUsuarioAtual()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    return httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                           httpContext.User.FindFirst("UsuarioId")?.Value ??
                           httpContext.User.FindFirst("sub")?.Value;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private string? ObterIdEmpresaAtual()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                return httpContext?.Items["EmpresaId"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private string ObterIpAddress()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    // Verificar se há proxy (X-Forwarded-For)
                    var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(forwardedFor))
                    {
                        return forwardedFor.Split(',')[0].Trim();
                    }

                    // Verificar se há proxy (X-Real-IP)
                    var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(realIp))
                    {
                        return realIp;
                    }

                    // IP direto
                    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconhecido";
                }
                return "Desconhecido";
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
                return httpContext?.Request.Headers["User-Agent"].FirstOrDefault() ?? "Desconhecido";
            }
            catch
            {
                return "Desconhecido";
            }
        }

        #endregion

        #region Validações

        public override async Task ValidarEntidadeAsync(LogADM entity)
        {
            // LogADM não precisa de validações específicas além das básicas
            await Task.CompletedTask;
        }

        #endregion
    }
}

