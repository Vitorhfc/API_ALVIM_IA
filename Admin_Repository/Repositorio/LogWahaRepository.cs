using Admin_Repository.Configuration;
using Admin_Repository.Repositorio.Interface;
using Admin_Repository.RepositorioGenerico;
using Shared.Classes.Entidades.ADM;

namespace Admin_Repository.Repositorio
{
    /// <summary>
    /// Repositório específico para operações com a entidade LogWaha
    /// </summary>
    public class LogWahaRepository : RepositorioGenerico<LogWaha>, ILogWahaRepository
    {
        public LogWahaRepository(ContextBaseAdmin contextBaseAdmin)
            : base(contextBaseAdmin, "LogWaha")
        {
        }

        #region Métodos Específicos

        /// <summary>
        /// Busca logs por empresa
        /// </summary>
        public async Task<IEnumerable<LogWaha>> BuscarPorEmpresaAsync(string empresaId, int page = 1, int pageSize = 50)
        {
            try
            {
                var resultado = await BuscarPorFiltroPaginadoAsync(
                    l => l.EmpresaId == empresaId && l.FlgAtivo,
                    page,
                    pageSize);

                return resultado.Where(l => l != null).Cast<LogWaha>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs por empresa '{empresaId}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca logs por ID de log
        /// </summary>
        public async Task<IEnumerable<LogWaha>> BuscarPorIdLogAsync(string idLog)
        {
            try
            {
                var resultado = await BuscarPorFiltroAsync(l => l.IdLog == idLog && l.FlgAtivo);
                return resultado.Where(l => l != null).Cast<LogWaha>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs por ID '{idLog}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca logs por tipo de evento
        /// </summary>
        public async Task<IEnumerable<LogWaha>> BuscarPorTipoEventoAsync(string tipoEvento, int page = 1, int pageSize = 50)
        {
            try
            {
                var resultado = await BuscarPorFiltroPaginadoAsync(
                    l => l.TipoEvento == tipoEvento && l.FlgAtivo,
                    page,
                    pageSize);

                return resultado.Where(l => l != null).Cast<LogWaha>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs por tipo de evento '{tipoEvento}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca logs por telefone de origem
        /// </summary>
        public async Task<IEnumerable<LogWaha>> BuscarPorTelefoneAsync(string telefone, int page = 1, int pageSize = 50)
        {
            try
            {
                var resultado = await BuscarPorFiltroPaginadoAsync(
                    l => (l.TelefoneOrigem == telefone || l.TelefoneDestino == telefone) && l.FlgAtivo,
                    page,
                    pageSize);

                return resultado.Where(l => l != null).Cast<LogWaha>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs por telefone '{telefone}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca logs por período
        /// </summary>
        public async Task<IEnumerable<LogWaha>> BuscarPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, int page = 1, int pageSize = 50)
        {
            try
            {
                var resultado = await BuscarPorFiltroPaginadoAsync(
                    l => l.DtaCadastro >= dataInicio && l.DtaCadastro <= dataFim && l.FlgAtivo,
                    page,
                    pageSize);

                return resultado.Where(l => l != null).Cast<LogWaha>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs por período: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca logs com erro
        /// </summary>
        public async Task<IEnumerable<LogWaha>> BuscarLogsComErroAsync(int page = 1, int pageSize = 50)
        {
            try
            {
                var resultado = await BuscarPorFiltroPaginadoAsync(
                    l => !l.Sucesso && l.FlgAtivo,
                    page,
                    pageSize);

                return resultado.Where(l => l != null).Cast<LogWaha>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs com erro: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Limpa logs antigos (mais de X dias)
        /// </summary>
        public async Task<int> LimparLogsAntigosAsync(int dias = 90)
        {
            try
            {
                var dataLimite = DateTime.Now.AddDays(-dias);
                var logsAntigos = await BuscarPorFiltroAsync(l => l.DtaCadastro < dataLimite);

                var logsValidos = logsAntigos.Where(l => l != null).Cast<LogWaha>();
                var count = logsValidos.Count();

                if (count > 0)
                {
                    foreach (var log in logsValidos)
                    {
                        if (log != null)
                        {
                            log.FlgAtivo = false; // Soft delete
                            await EditarAsync(log);
                        }
                    }
                }

                return count;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao limpar logs antigos: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
