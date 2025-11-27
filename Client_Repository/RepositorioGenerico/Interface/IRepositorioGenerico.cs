using MongoDB.Driver;
using System.Linq.Expressions;

namespace Client_Repository.RepositorioGenerico.Interface
{
    public interface IRepositorioGenerico<TEntidade> where TEntidade : class
    {
        Task<TEntidade> AdicionarAsync(TEntidade entidade);
        Task<List<TEntidade>> AdicionarVariosAsync(IEnumerable<TEntidade> entidades);
        Task<List<TEntidade>> AdicionarArrayAsync(List<TEntidade> array);
        Task<TEntidade> AtualizarAsync(TEntidade entidade);
        Task<TEntidade> EditarAsync(TEntidade objeto);
        Task<List<TEntidade>> EditarArrayAsync(List<TEntidade> array);
        Task<bool> AtualizarCampoAsync(string id, string nomeCampo, object valor);
        Task<bool> DeletarAsync(string id);
        Task ExcluirAsync(TEntidade objeto);
        Task<bool> DeletarVariosAsync(Expression<Func<TEntidade, bool>> filtro);
        Task<TEntidade> ObterPorIdAsync(string id);
        Task<TEntidade> BuscarPorIdAsync(string id);
        Task<List<TEntidade>> ObterTodosAsync();
        Task<IEnumerable<TEntidade>> BuscarTodosAsync();
        Task<List<TEntidade>> ObterPorFiltroAsync(Expression<Func<TEntidade, bool>> filtro);
        Task<IEnumerable<TEntidade>> BuscarPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression);
        Task<TEntidade> ObterUnicoPorFiltroAsync(Expression<Func<TEntidade, bool>> filtro);
        Task<TEntidade> BuscarPrimeiroPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression);
        Task<long> ContarAsync(Expression<Func<TEntidade, bool>> filtro = null);
        Task<int> BuscarContagemTotalAsync();
        Task<int> BuscarContagemTotalPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression);
        Task<bool> ExisteAsync(Expression<Func<TEntidade, bool>> filtro);
        Task<List<TEntidade>> ObterComPaginacaoAsync(
            Expression<Func<TEntidade, bool>> filtro,
            int pagina,
            int tamanhoPagina,
            Expression<Func<TEntidade, object>> ordenarPor = null,
            bool descendente = false);
        Task<IEnumerable<TEntidade>> BuscarPorFiltroPaginadoAsync(
            Expression<Func<TEntidade?, bool>> expression,
            int page,
            int pageSize);
        IMongoCollection<TEntidade> ObterColecao();
    }
}