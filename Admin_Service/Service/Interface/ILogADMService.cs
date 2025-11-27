using Admin_Service.ServiceGenerico.Interface;
using Shared.Classes.Entidades.ADM;

namespace Admin_Service.Service.Interface
{
    /// <summary>
    /// Interface para operações específicas com a entidade LogADM
    /// </summary>
    public interface ILogADMService : IServiceGenerico<LogADM>
    {
        #region Consultas Específicas

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
        /// <returns>Log criado</returns>
        Task<LogADM> LogRequisicaoAsync(string metodo, string endpoint, string acao, object? dadosEnviados = null, bool sucesso = true, string? descricao = null);

        /// <summary>
        /// Registra um erro no sistema
        /// </summary>
        /// <param name="ex">Exceção ocorrida</param>
        /// <param name="metodo">Nome do método</param>
        /// <param name="controller">Nome do controller</param>
        /// <param name="variaveis">Variáveis relacionadas ao erro</param>
        /// <returns>Log de erro criado</returns>
        Task<LogADM> LogErroAsync(Exception ex, string metodo, string controller, string? variaveis = null);

        /// <summary>
        /// Registra uma ação do usuário
        /// </summary>
        /// <param name="acao">Ação realizada</param>
        /// <param name="dadoAntigo">Dados antes da alteração</param>
        /// <param name="dadoNovo">Dados após a alteração</param>
        /// <param name="descricao">Descrição adicional</param>
        /// <returns>Log de ação criado</returns>
        Task<LogADM> LogAcaoAsync(string acao, string? dadoAntigo = null, string? dadoNovo = null, string? descricao = null);

        /// <summary>
        /// Registra informações gerais
        /// </summary>
        /// <param name="mensagem">Mensagem a ser registrada</param>
        /// <param name="metodo">Nome do método</param>
        /// <param name="controller">Nome do controller</param>
        /// <returns>Log de informação criado</returns>
        Task<LogADM> LogInfoAsync(string mensagem, string metodo, string controller);

        #endregion
    }
}

