using Client.Controllers.Base;
using Client_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LogController : ControllerBaseClient<LogController>
    {
        #region Campos

        private readonly ILogClientRepositorio _logClientRepositorio;
        private readonly Admin_Repository.Repositorio.Interface.ILogWahaRepository _logWahaRepository;
        private readonly ILogClientService _logClientService;
        private readonly ILogger<LogController> _logger;
        private readonly Client_Repository.Configuration.Contexto.Interface.IContextoMultiTenantService _contextoMultiTenant;

        #endregion

        #region Construtor

        public LogController(
            ILogClientRepositorio logClientRepositorio,
            Admin_Repository.Repositorio.Interface.ILogWahaRepository logWahaRepository,
            ILogClientService logClientService,
            ILogger<LogController> logger,
            Client_Repository.Configuration.Contexto.Interface.IContextoMultiTenantService contextoMultiTenant)
            : base(logClientService, logger)
        {
            _logClientRepositorio = logClientRepositorio;
            _logWahaRepository = logWahaRepository;
            _logClientService = logClientService;
            _logger = logger;
            _contextoMultiTenant = contextoMultiTenant;
        }

        #endregion

        #region Endpoints - LogWaha

        /// <summary>
        /// Lista todos os logs do WAHA filtrados pela empresa do usuário
        /// </summary>
        /// <param name="page">Número da página (padrão: 1)</param>
        /// <param name="pageSize">Tamanho da página (padrão: 50, máximo: 100)</param>
        /// <returns>Lista paginada de logs do WAHA</returns>
        [HttpGet("waha")]
        public async Task<IActionResult> ListarLogsWaha([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validar parâmetros
                if (page < 1)
                    return Erro("O número da página deve ser maior que zero");

                if (pageSize < 1 || pageSize > 100)
                    return Erro("O tamanho da página deve estar entre 1 e 100");

                // Obter empresa do contexto
                var empresaId = _contextoMultiTenant.ObterIdEmpresaAtual();
                if (string.IsNullOrWhiteSpace(empresaId))
                    return Erro("Não foi possível identificar a empresa do usuário");

                // Buscar logs filtrados pela empresa
                var logs = await _logWahaRepository.BuscarPorEmpresaAsync(empresaId, page, pageSize);

                await LogInfoAsync($"Listou {logs.Count()} logs do WAHA - Página: {page}, Tamanho: {pageSize}", nameof(ListarLogsWaha));

                return Sucesso(new
                {
                    page,
                    pageSize,
                    totalItems = logs.Count(),
                    items = logs
                }, "Logs do WAHA listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarLogsWaha), $"Página: {page}, Tamanho: {pageSize}");
            }
        }

        /// <summary>
        /// Busca logs do WAHA por ID de log
        /// </summary>
        /// <param name="idLog">ID do log</param>
        /// <returns>Logs relacionados ao ID especificado</returns>
        [HttpGet("waha/{idLog}")]
        public async Task<IActionResult> BuscarLogsWahaPorId(string idLog)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idLog))
                    return Erro("ID do log é obrigatório");

                // Obter empresa do contexto para validação
                var empresaId = _contextoMultiTenant.ObterIdEmpresaAtual();
                if (string.IsNullOrWhiteSpace(empresaId))
                    return Erro("Não foi possível identificar a empresa do usuário");

                var logs = await _logWahaRepository.BuscarPorIdLogAsync(idLog);

                // Filtrar apenas logs da empresa do usuário
                var logsFiltrados = logs.Where(l => l.EmpresaId == empresaId).ToList();

                if (!logsFiltrados.Any())
                {
                    await LogInfoAsync($"Nenhum log encontrado para o ID: {idLog}", nameof(BuscarLogsWahaPorId));
                    return NaoEncontrado("Nenhum log encontrado para o ID especificado");
                }

                await LogInfoAsync($"Encontrou {logsFiltrados.Count} logs para o ID: {idLog}", nameof(BuscarLogsWahaPorId));

                return Sucesso(logsFiltrados, "Logs encontrados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(BuscarLogsWahaPorId), $"IdLog: {idLog}");
            }
        }

        /// <summary>
        /// Busca logs do WAHA com erro
        /// </summary>
        /// <param name="page">Número da página (padrão: 1)</param>
        /// <param name="pageSize">Tamanho da página (padrão: 50, máximo: 100)</param>
        /// <returns>Lista paginada de logs com erro</returns>
        [HttpGet("waha/erros")]
        public async Task<IActionResult> ListarLogsWahaComErro([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validar parâmetros
                if (page < 1)
                    return Erro("O número da página deve ser maior que zero");

                if (pageSize < 1 || pageSize > 100)
                    return Erro("O tamanho da página deve estar entre 1 e 100");

                // Obter empresa do contexto
                var empresaId = _contextoMultiTenant.ObterIdEmpresaAtual();
                if (string.IsNullOrWhiteSpace(empresaId))
                    return Erro("Não foi possível identificar a empresa do usuário");

                // Buscar logs com erro
                var logsComErro = await _logWahaRepository.BuscarLogsComErroAsync(page, pageSize);

                // Filtrar apenas logs da empresa do usuário
                var logsFiltrados = logsComErro.Where(l => l.EmpresaId == empresaId).ToList();

                await LogInfoAsync($"Listou {logsFiltrados.Count} logs com erro do WAHA - Página: {page}, Tamanho: {pageSize}", nameof(ListarLogsWahaComErro));

                return Sucesso(new
                {
                    page,
                    pageSize,
                    totalItems = logsFiltrados.Count,
                    items = logsFiltrados
                }, "Logs com erro listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarLogsWahaComErro), $"Página: {page}, Tamanho: {pageSize}");
            }
        }

        #endregion

        #region Endpoints - LogClient

        /// <summary>
        /// Lista todos os logs do cliente (sem filtro de empresa)
        /// </summary>
        /// <param name="page">Número da página (padrão: 1)</param>
        /// <param name="pageSize">Tamanho da página (padrão: 50, máximo: 100)</param>
        /// <returns>Lista paginada de logs do cliente</returns>
        [HttpGet("client")]
        public async Task<IActionResult> ListarLogsClient([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validar parâmetros
                if (page < 1)
                    return Erro("O número da página deve ser maior que zero");

                if (pageSize < 1 || pageSize > 100)
                    return Erro("O tamanho da página deve estar entre 1 e 100");

                // Buscar todos os logs do cliente (sem filtro de empresa)
                var logs = await _logClientRepositorio.BuscarTodosAsync();

                // Aplicar paginação manual
                var logsPaginados = logs
                    .OrderByDescending(l => l.DtaCadastro)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var totalLogs = logs.Count();

                await LogInfoAsync($"Listou {logsPaginados.Count} logs do cliente - Página: {page}, Tamanho: {pageSize}, Total: {totalLogs}", nameof(ListarLogsClient));

                return Sucesso(new
                {
                    page,
                    pageSize,
                    totalItems = totalLogs,
                    totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize),
                    items = logsPaginados
                }, "Logs do cliente listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarLogsClient), $"Página: {page}, Tamanho: {pageSize}");
            }
        }

        /// <summary>
        /// Busca log do cliente por ID
        /// </summary>
        /// <param name="id">ID do log</param>
        /// <returns>Log do cliente</returns>
        [HttpGet("client/{id}")]
        public async Task<IActionResult> BuscarLogClientPorId(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do log é obrigatório");

                var log = await _logClientRepositorio.BuscarPorIdAsync(id);

                if (log == null)
                {
                    await LogInfoAsync($"Log não encontrado - ID: {id}", nameof(BuscarLogClientPorId));
                    return NaoEncontrado("Log não encontrado");
                }

                await LogInfoAsync($"Log encontrado - ID: {id}", nameof(BuscarLogClientPorId));

                return Sucesso(log, "Log encontrado com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(BuscarLogClientPorId), $"ID: {id}");
            }
        }

        /// <summary>
        /// Lista logs do cliente por tipo
        /// </summary>
        /// <param name="tipo">Tipo de log (1=Sistema, 2=Integracao, 3=Processamento, 4=Erro, 5=Webhook)</param>
        /// <param name="page">Número da página (padrão: 1)</param>
        /// <param name="pageSize">Tamanho da página (padrão: 50, máximo: 100)</param>
        /// <returns>Lista paginada de logs do cliente por tipo</returns>
        [HttpGet("client/tipo/{tipo}")]
        public async Task<IActionResult> ListarLogsClientPorTipo(int tipo, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validar parâmetros
                if (page < 1)
                    return Erro("O número da página deve ser maior que zero");

                if (pageSize < 1 || pageSize > 100)
                    return Erro("O tamanho da página deve estar entre 1 e 100");

                if (tipo < 1 || tipo > 5)
                    return Erro("Tipo de log inválido (deve ser entre 1 e 5)");

                // Buscar logs por tipo
                var logs = await _logClientRepositorio.BuscarPorFiltroAsync(l => l.Tipo == (Shared.Classes.Entidades.Client.TipoLog)tipo);

                // Aplicar paginação manual
                var logsPaginados = logs
                    .OrderByDescending(l => l.DtaCadastro)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var totalLogs = logs.Count();

                await LogInfoAsync($"Listou {logsPaginados.Count} logs do tipo {tipo} - Página: {page}, Tamanho: {pageSize}, Total: {totalLogs}", nameof(ListarLogsClientPorTipo));

                return Sucesso(new
                {
                    page,
                    pageSize,
                    tipo,
                    totalItems = totalLogs,
                    totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize),
                    items = logsPaginados
                }, "Logs do cliente por tipo listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarLogsClientPorTipo), $"Tipo: {tipo}, Página: {page}, Tamanho: {pageSize}");
            }
        }

        #endregion
    }
}
