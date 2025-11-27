using ADM.Controllers.Base;
using Admin_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LogADMController : ControllerBaseComplemento<LogADMController>
    {
        private readonly ILogADMService _logADMService;

        public LogADMController(ILogADMService logADMService, ILogger<LogADMController> logger)
            : base(logADMService, logger)
        {
            _logADMService = logADMService;
        }

        [HttpGet]
        public async Task<IActionResult> ListarLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var logs = await _logADMService.BuscarPorFiltroPaginadoAsync(l => l.FlgAtivo, page, pageSize);

                await LogInfoAsync($"Listou logs - Página: {page}, Tamanho: {pageSize}", nameof(ListarLogs));

                return Sucesso(logs, "Logs listados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ListarLogs), $"Página: {page}, PageSize: {pageSize}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarLogPorId(string id)
        {
            try
            {
                var log = await _logADMService.BuscarPorIdAsync(id);

                if (log == null)
                {
                    await LogInfoAsync($"Tentativa de buscar log inexistente: {id}", nameof(BuscarLogPorId));
                    return Erro("Log não encontrado");
                }

                await LogInfoAsync($"Buscou log: {log.Acao}", nameof(BuscarLogPorId));

                return Sucesso(log, "Log encontrado");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(BuscarLogPorId), $"ID: {id}");
            }
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ListarLogsPorUsuario(string usuarioId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var logs = await _logADMService.BuscarPorUsuarioAsync(usuarioId, page, pageSize);

                await LogInfoAsync($"Listou logs do usuário {usuarioId} - Página: {page}", nameof(ListarLogsPorUsuario));

                return Sucesso(logs, $"Logs do usuário {usuarioId} listados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ListarLogsPorUsuario), $"UsuarioId: {usuarioId}");
            }
        }

        [HttpGet("empresa/{empresaId}")]
        public async Task<IActionResult> ListarLogsPorEmpresa(string empresaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var logs = await _logADMService.BuscarPorEmpresaAsync(empresaId, page, pageSize);

                await LogInfoAsync($"Listou logs da empresa {empresaId} - Página: {page}", nameof(ListarLogsPorEmpresa));

                return Sucesso(logs, $"Logs da empresa {empresaId} listados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ListarLogsPorEmpresa), $"EmpresaId: {empresaId}");
            }
        }

        [HttpGet("periodo")]
        public async Task<IActionResult> ListarLogsPorPeriodo(
            [FromQuery] DateTime dataInicio,
            [FromQuery] DateTime dataFim,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (dataInicio > dataFim)
                {
                    return Erro("Data de início deve ser anterior à data de fim");
                }

                var dataFimCompleta = dataFim.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                var logs = await _logADMService.BuscarPorPeriodoAsync(dataInicio.Date, dataFimCompleta, page, pageSize);

                await LogInfoAsync($"Listou logs do período {dataInicio:yyyy-MM-dd} a {dataFim:yyyy-MM-dd} - Página: {page}", nameof(ListarLogsPorPeriodo));

                return Sucesso(logs, $"Logs do período {dataInicio:yyyy-MM-dd} a {dataFim:yyyy-MM-dd} listados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ListarLogsPorPeriodo),
                    $"DataInicio: {dataInicio:yyyy-MM-dd}, DataFim: {dataFim:yyyy-MM-dd}");
            }
        }

        [HttpGet("acao/{acao}")]
        public async Task<IActionResult> ListarLogsPorAcao(string acao, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var logs = await _logADMService.BuscarPorAcaoAsync(acao, page, pageSize);

                await LogInfoAsync($"Listou logs da ação '{acao}' - Página: {page}", nameof(ListarLogsPorAcao));

                return Sucesso(logs, $"Logs da ação '{acao}' listados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ListarLogsPorAcao), $"Acao: {acao}");
            }
        }

        [HttpGet("erros")]
        public async Task<IActionResult> ListarLogsDeErro([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var logs = await _logADMService.BuscarPorFiltroPaginadoAsync(l => !l.Sucesso && l.FlgAtivo, page, pageSize);

                await LogInfoAsync($"Listou logs de erro - Página: {page}", nameof(ListarLogsDeErro));

                return Sucesso(logs, "Logs de erro listados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ListarLogsDeErro));
            }
        }

        [HttpGet("sucessos")]
        public async Task<IActionResult> ListarLogsDeSucesso([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var logs = await _logADMService.BuscarPorFiltroPaginadoAsync(l => l.Sucesso && l.FlgAtivo, page, pageSize);

                await LogInfoAsync($"Listou logs de sucesso - Página: {page}", nameof(ListarLogsDeSucesso));

                return Sucesso(logs, "Logs de sucesso listados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ListarLogsDeSucesso));
            }
        }

        [HttpGet("ip/{ip}")]
        public async Task<IActionResult> ListarLogsPorIP(string ip, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var logs = await _logADMService.BuscarPorFiltroPaginadoAsync(l => l.IpAddress == ip && l.FlgAtivo, page, pageSize);

                await LogInfoAsync($"Listou logs do IP {ip} - Página: {page}", nameof(ListarLogsPorIP));

                return Sucesso(logs, $"Logs do IP {ip} listados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ListarLogsPorIP), $"IP: {ip}");
            }
        }

        [HttpGet("estatisticas")]
        public async Task<IActionResult> ObterEstatisticas()
        {
            try
            {
                var totalLogs = await _logADMService.BuscarContagemTotalAsync();
                var logsSucesso = await _logADMService.BuscarContagemTotalPorFiltroAsync(l => l.Sucesso && l.FlgAtivo);
                var logsErro = await _logADMService.BuscarContagemTotalPorFiltroAsync(l => !l.Sucesso && l.FlgAtivo);

                var hoje = DateTime.Today;
                var logsHoje = await _logADMService.BuscarContagemTotalPorFiltroAsync(l =>
                    l.DtaCadastro == hoje && l.FlgAtivo);

                var estatisticas = new
                {
                    totalLogs,
                    logsSucesso,
                    logsErro,
                    logsHoje,
                    percentualSucesso = totalLogs > 0 ? (double)logsSucesso / totalLogs * 100 : 0,
                    percentualErro = totalLogs > 0 ? (double)logsErro / totalLogs * 100 : 0
                };

                await LogInfoAsync("Consultou estatísticas dos logs", nameof(ObterEstatisticas));

                return Sucesso(estatisticas, "Estatísticas obtidas com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ObterEstatisticas));
            }
        }

        [HttpDelete("limpar-antigos")]
        public async Task<IActionResult> LimparLogsAntigos([FromQuery] int? dias)
        {
            try
            {
                if (!IsAdministrador())
                {
                    await LogInfoAsync("Tentativa de limpeza de logs por usuário não administrador", nameof(LimparLogsAntigos));
                    return Erro("Apenas administradores podem limpar logs antigos");
                }

                var diasParaManter = dias ?? 90;

                var logsRemovidos = await _logADMService.LimparLogsAntigosAsync(diasParaManter);

                await RegistraAcaoAsync(
                    "Limpar Logs Antigos",
                    $"Logs mais antigos que {diasParaManter} dias",
                    $"{logsRemovidos} logs removidos",
                    $"Limpeza de logs realizada - {logsRemovidos} registros removidos");

                return Sucesso(new { logsRemovidos }, $"{logsRemovidos} logs antigos foram removidos");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(LimparLogsAntigos), $"Dias: {dias}");
            }
        }
    }
}