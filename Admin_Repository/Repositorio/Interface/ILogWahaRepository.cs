using Admin_Repository.RepositorioGenerico.Interface;
using Shared.Classes.Entidades.ADM;

namespace Admin_Repository.Repositorio.Interface
{
    /// <summary>
    /// Interface para operações específicas com a entidade LogWaha
    /// </summary>
    public interface ILogWahaRepository : IRepositorioGenerico<LogWaha>
    {
        /// <summary>
        /// Busca logs por empresa
        /// </summary>
        /// <param name="empresaId">ID da empresa</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs</returns>
        Task<IEnumerable<LogWaha>> BuscarPorEmpresaAsync(string empresaId, int page = 1, int pageSize = 50);

        /// <summary>
        /// Busca logs por ID de log
        /// </summary>
        /// <param name="idLog">ID do log</param>
        /// <returns>Logs com o ID especificado</returns>
        Task<IEnumerable<LogWaha>> BuscarPorIdLogAsync(string idLog);

        /// <summary>
        /// Busca logs por tipo de evento
        /// </summary>
        /// <param name="tipoEvento">Tipo de evento (ex: "ENTRADA", "SAIDA_SUCESSO")</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs</returns>
        Task<IEnumerable<LogWaha>> BuscarPorTipoEventoAsync(string tipoEvento, int page = 1, int pageSize = 50);

        /// <summary>
        /// Busca logs por telefone de origem
        /// </summary>
        /// <param name="telefone">Número de telefone</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs</returns>
        Task<IEnumerable<LogWaha>> BuscarPorTelefoneAsync(string telefone, int page = 1, int pageSize = 50);

        /// <summary>
        /// Busca logs por período
        /// </summary>
        /// <param name="dataInicio">Data de início</param>
        /// <param name="dataFim">Data de fim</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs</returns>
        Task<IEnumerable<LogWaha>> BuscarPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, int page = 1, int pageSize = 50);

        /// <summary>
        /// Busca logs com erro
        /// </summary>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de logs com erro</returns>
        Task<IEnumerable<LogWaha>> BuscarLogsComErroAsync(int page = 1, int pageSize = 50);

        /// <summary>
        /// Limpa logs antigos (mais de X dias)
        /// </summary>
        /// <param name="dias">Quantidade de dias para manter</param>
        /// <returns>Quantidade de logs removidos</returns>
        Task<int> LimparLogsAntigosAsync(int dias = 90);
    }
}
