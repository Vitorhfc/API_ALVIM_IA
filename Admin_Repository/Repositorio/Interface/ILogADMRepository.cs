using Admin_Repository.RepositorioGenerico.Interface;
using Shared.Classes.Entidades.ADM;

namespace Admin_Repository.Repositorio.Interface
{
    /// <summary>
    /// Interface para operações específicas com a entidade LogADM
    /// </summary>
    public interface ILogADMRepository : IRepositorioGenerico<LogADM>
    {
        /// <summary>
        /// Busca logs por usuário
        /// </summary>
        /// <param name="usuarioId">ID do usuário</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs</returns>
        Task<IEnumerable<LogADM>> BuscarPorUsuarioAsync(string usuarioId, int page = 1, int pageSize = 50);

        /// <summary>
        /// Busca logs por empresa
        /// </summary>
        /// <param name="empresaId">ID da empresa</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs</returns>
        Task<IEnumerable<LogADM>> BuscarPorEmpresaAsync(string empresaId, int page = 1, int pageSize = 50);

        /// <summary>
        /// Busca logs por período
        /// </summary>
        /// <param name="dataInicio">Data de início</param>
        /// <param name="dataFim">Data de fim</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs</returns>
        Task<IEnumerable<LogADM>> BuscarPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, int page = 1, int pageSize = 50);

        /// <summary>
        /// Busca logs por ação
        /// </summary>
        /// <param name="acao">Ação realizada</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs</returns>
        Task<IEnumerable<LogADM>> BuscarPorAcaoAsync(string acao, int page = 1, int pageSize = 50);

        /// <summary>
        /// Limpa logs antigos (mais de X dias)
        /// </summary>
        /// <param name="dias">Quantidade de dias para manter</param>
        /// <returns>Quantidade de logs removidos</returns>
        Task<int> LimparLogsAntigosAsync(int dias = 90);
    }
}

