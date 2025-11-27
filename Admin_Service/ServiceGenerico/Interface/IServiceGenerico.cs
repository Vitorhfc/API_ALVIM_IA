using System.Linq.Expressions;

namespace Admin_Service.ServiceGenerico.Interface
{
    /// <summary>
    /// Interface genérica para operações de serviço no contexto administrativo
    /// </summary>
    /// <typeparam name="TEntidade">Tipo da entidade</typeparam>
    public interface IServiceGenerico<TEntidade> where TEntidade : class
    {
        #region Consultas

        /// <summary>
        /// Busca todas as entidades
        /// </summary>
        /// <returns>Lista de todas as entidades</returns>
        Task<IEnumerable<TEntidade?>> BuscarTodosAsync();

        /// <summary>
        /// Busca entidades por filtro
        /// </summary>
        /// <param name="expression">Expressão de filtro</param>
        /// <returns>Lista de entidades que atendem ao filtro</returns>
        Task<IEnumerable<TEntidade?>> BuscarPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression);

        /// <summary>
        /// Busca entidades por filtro com paginação
        /// </summary>
        /// <param name="expression">Expressão de filtro</param>
        /// <param name="page">Número da página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista paginada de entidades</returns>
        Task<IEnumerable<TEntidade?>> BuscarPorFiltroPaginadoAsync(Expression<Func<TEntidade?, bool>> expression, int page, int pageSize);

        /// <summary>
        /// Busca entidade por ID
        /// </summary>
        /// <param name="id">ID da entidade</param>
        /// <returns>Entidade encontrada ou null</returns>
        Task<TEntidade?> BuscarPorIdAsync(string? id);

        /// <summary>
        /// Conta total de entidades que atendem ao filtro
        /// </summary>
        /// <param name="expression">Expressão de filtro</param>
        /// <returns>Quantidade de entidades</returns>
        Task<int> BuscarContagemTotalPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression);

        /// <summary>
        /// Conta total de entidades
        /// </summary>
        /// <returns>Quantidade total de entidades</returns>
        Task<int> BuscarContagemTotalAsync();

        /// <summary>
        /// Busca a primeira entidade que atende ao filtro
        /// </summary>
        /// <param name="expression">Expressão de filtro</param>
        /// <returns>Primeira entidade encontrada ou null</returns>
        Task<TEntidade?> BuscarPrimeiroPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression);

        #endregion

        #region Operações

        /// <summary>
        /// Adiciona uma nova entidade
        /// </summary>
        /// <param name="entity">Entidade a ser adicionada</param>
        /// <returns>Entidade adicionada</returns>
        Task<TEntidade?> AdicionarAsync(TEntidade entity);

        /// <summary>
        /// Adiciona múltiplas entidades
        /// </summary>
        /// <param name="entities">Lista de entidades a serem adicionadas</param>
        /// <returns>Lista de entidades adicionadas</returns>
        Task<List<TEntidade>> AdicionarArrayAsync(List<TEntidade> entities);

        /// <summary>
        /// Edita uma entidade existente
        /// </summary>
        /// <param name="entity">Entidade com dados atualizados</param>
        /// <returns>Entidade editada</returns>
        Task<TEntidade?> EditarAsync(TEntidade entity);

        /// <summary>
        /// Edita múltiplas entidades
        /// </summary>
        /// <param name="entities">Lista de entidades a serem editadas</param>
        /// <returns>Lista de entidades editadas</returns>
        Task<List<TEntidade>> EditarArrayAsync(List<TEntidade> entities);

        /// <summary>
        /// Exclui uma entidade
        /// </summary>
        /// <param name="entity">Entidade a ser excluída</param>
        Task ExcluirAsync(TEntidade entity);

        /// <summary>
        /// Exclui uma entidade por ID
        /// </summary>
        /// <param name="id">ID da entidade a ser excluída</param>
        Task ExcluirPorIdAsync(string id);

        #endregion

        #region Validações

        /// <summary>
        /// Valida uma entidade antes de operações de persistência
        /// </summary>
        /// <param name="entity">Entidade a ser validada</param>
        /// <returns>Task de validação</returns>
        Task ValidarEntidadeAsync(TEntidade entity);

        #endregion
    }
}
