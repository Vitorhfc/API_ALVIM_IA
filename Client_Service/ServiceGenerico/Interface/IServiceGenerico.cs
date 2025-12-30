using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Client_Service.ServiceGenerico.Interface
{
    public interface IServiceGenerico<TEntidade> where TEntidade : class
    {
        Task<IEnumerable<TEntidade?>> BuscarTodosAsync();
        Task<IEnumerable<TEntidade?>> BuscarPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression);
        Task<IEnumerable<TEntidade?>> BuscarPorFiltroPaginadoAsync(Expression<Func<TEntidade?, bool>> expression, int page, int pageSize);
        Task<TEntidade?> BuscarPorIdAsync(string? id);
        Task<int> BuscarContagemTotalPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression);
        Task<int> BuscarContagemTotalAsync();
        Task<TEntidade?> BuscarPrimeiroPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression);
        Task<TEntidade?> AdicionarAsync(TEntidade @object);
        Task<List<TEntidade>> AdicionarArrayAsync(List<TEntidade> array);
        Task<TEntidade?> EditarAsync(TEntidade objeto);
        Task<List<TEntidade>> EditarArrayAsync(List<TEntidade> array);
        Task ExcluirAsync(TEntidade @object);
        Task ExcluirPorIdAsync(string id);
    }
}
