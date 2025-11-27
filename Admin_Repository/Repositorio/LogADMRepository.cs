using Admin_Repository.Configuration;
using Admin_Repository.Repositorio.Interface;
using Admin_Repository.RepositorioGenerico;
using Shared.Classes.Entidades.ADM;

namespace Admin_Repository.Repositorio
{
    /// <summary>
    /// Repositório específico para operações com a entidade LogADM
    /// </summary>
    public class LogADMRepository : RepositorioGenerico<LogADM>, ILogADMRepository
    {
        public LogADMRepository(ContextBaseAdmin contextBaseAdmin) 
            : base(contextBaseAdmin, "LogADM")
        {
        }

        #region Métodos Específicos

        /// <summary>
        /// Busca logs por usuário
        /// </summary>
        /// <param name="usuarioId">ID do usuário</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs</returns>
        public async Task<IEnumerable<LogADM>> BuscarPorUsuarioAsync(string usuarioId, int page = 1, int pageSize = 50)
        {
            try
            {
                var resultado = await BuscarPorFiltroPaginadoAsync(
                    l => l.UsuarioId == usuarioId && l.FlgAtivo,
                    page,
                    pageSize);
                
                return resultado.Where(l => l != null).Cast<LogADM>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs por usuário '{usuarioId}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca logs por empresa
        /// </summary>
        /// <param name="empresaId">ID da empresa</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs</returns>
        public async Task<IEnumerable<LogADM>> BuscarPorEmpresaAsync(string empresaId, int page = 1, int pageSize = 50)
        {
            try
            {
                var resultado = await BuscarPorFiltroPaginadoAsync(
                    l => l.EmpresaId == empresaId && l.FlgAtivo,
                    page,
                    pageSize);
                
                return resultado.Where(l => l != null).Cast<LogADM>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs por empresa '{empresaId}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca logs por período
        /// </summary>
        /// <param name="dataInicio">Data de início</param>
        /// <param name="dataFim">Data de fim</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs</returns>
        public async Task<IEnumerable<LogADM>> BuscarPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, int page = 1, int pageSize = 50)
        {
            try
            {
                var resultado = await BuscarPorFiltroPaginadoAsync(
                    l => l.DtaCadastro >= dataInicio && l.DtaCadastro <= dataFim && l.FlgAtivo,
                    page,
                    pageSize);
                
                return resultado.Where(l => l != null).Cast<LogADM>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs por período: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca logs por ação
        /// </summary>
        /// <param name="acao">Ação realizada</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs</returns>
        public async Task<IEnumerable<LogADM>> BuscarPorAcaoAsync(string acao, int page = 1, int pageSize = 50)
        {
            try
            {
                var resultado = await BuscarPorFiltroPaginadoAsync(
                    l => l.Acao.Contains(acao) && l.FlgAtivo,
                    page,
                    pageSize);
                
                return resultado.Where(l => l != null).Cast<LogADM>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar logs por ação '{acao}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Limpa logs antigos (mais de X dias)
        /// </summary>
        /// <param name="dias">Quantidade de dias para manter</param>
        /// <returns>Quantidade de logs removidos</returns>
        public async Task<int> LimparLogsAntigosAsync(int dias = 90)
        {
            try
            {
                var dataLimite = DateTime.Now.AddDays(-dias);
                var logsAntigos = await BuscarPorFiltroAsync(l => l.DtaCadastro < dataLimite);

                var logsValidos = logsAntigos.Where(l => l != null).Cast<LogADM>();
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
