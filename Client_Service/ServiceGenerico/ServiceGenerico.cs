
using Client_Repository.RepositorioGenerico.Interface;
using Client_Service.ServiceGenerico.Interface;
using System.Linq.Expressions;

namespace Client_Service.ServiceGenerico
{
    public class ServiceGenerico<TEntidade> : IServiceGenerico<TEntidade>, IDisposable
        where TEntidade : class
    {
        protected readonly IRepositorioGenerico<TEntidade> _repositorio;
        private bool _disposed;

        public ServiceGenerico(IRepositorioGenerico<TEntidade> repositorio)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        }

        public virtual async Task<IEnumerable<TEntidade?>> BuscarTodosAsync()
        {
            try
            {
                return await _repositorio.BuscarTodosAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao buscar todos: {ex.Message}", ex);
            }
        }

        public virtual async Task<IEnumerable<TEntidade?>> BuscarPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(expression, nameof(expression));
                return await _repositorio.BuscarPorFiltroAsync(expression);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao buscar por filtro: {ex.Message}", ex);
            }
        }

        public virtual async Task<IEnumerable<TEntidade?>> BuscarPorFiltroPaginadoAsync(
            Expression<Func<TEntidade?, bool>> expression,
            int page,
            int pageSize)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(expression, nameof(expression));

                if (page <= 0)
                    throw new ArgumentException("Página deve ser maior que zero", nameof(page));

                if (pageSize <= 0)
                    throw new ArgumentException("Tamanho da página deve ser maior que zero", nameof(pageSize));

                return await _repositorio.BuscarPorFiltroPaginadoAsync(expression, page, pageSize);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao buscar paginado: {ex.Message}", ex);
            }
        }

        public virtual async Task<TEntidade?> BuscarPorIdAsync(string? id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("ID não pode ser nulo ou vazio", nameof(id));

                return await _repositorio.BuscarPorIdAsync(id);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao buscar por ID: {ex.Message}", ex);
            }
        }

        public virtual async Task<int> BuscarContagemTotalPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(expression, nameof(expression));
                return await _repositorio.BuscarContagemTotalPorFiltroAsync(expression);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao contar por filtro: {ex.Message}", ex);
            }
        }

        public virtual async Task<int> BuscarContagemTotalAsync()
        {
            try
            {
                return await _repositorio.BuscarContagemTotalAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao contar total: {ex.Message}", ex);
            }
        }

        public virtual async Task<TEntidade?> BuscarPrimeiroPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(expression, nameof(expression));
                return await _repositorio.BuscarPrimeiroPorFiltroAsync(expression);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao buscar primeiro: {ex.Message}", ex);
            }
        }

        public virtual async Task<TEntidade?> AdicionarAsync(TEntidade @object)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(@object, nameof(@object));
                await ValidarEntidade(@object);
                return await _repositorio.AdicionarAsync(@object);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao adicionar: {ex.Message}", ex);
            }
        }

        public virtual async Task<List<TEntidade>> AdicionarArrayAsync(List<TEntidade> array)
        {
            try
            {
                if (array == null || array.Count == 0)
                    throw new ArgumentException("Array não pode ser nulo ou vazio", nameof(array));

                foreach (var item in array)
                    await ValidarEntidade(item);

                return await _repositorio.AdicionarArrayAsync(array);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao adicionar array: {ex.Message}", ex);
            }
        }

        public virtual async Task<TEntidade?> EditarAsync(TEntidade objeto)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(objeto, nameof(objeto));
                await ValidarEntidade(objeto);
                return await _repositorio.EditarAsync(objeto);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao editar: {ex.Message}", ex);
            }
        }

        public virtual async Task<List<TEntidade>> EditarArrayAsync(List<TEntidade> array)
        {
            try
            {
                if (array == null || array.Count == 0)
                    throw new ArgumentException("Array não pode ser nulo ou vazio", nameof(array));

                foreach (var item in array)
                    await ValidarEntidade(item);

                return await _repositorio.EditarArrayAsync(array);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao editar array: {ex.Message}", ex);
            }
        }

        public virtual async Task ExcluirAsync(TEntidade @object)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(@object, nameof(@object));
                await _repositorio.ExcluirAsync(@object);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao excluir: {ex.Message}", ex);
            }
        }

        public virtual async Task ExcluirPorIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("ID não pode ser nulo ou vazio", nameof(id));

                var entidade = await _repositorio.BuscarPorIdAsync(id);
                if (entidade == null)
                    throw new InvalidOperationException($"Entidade com ID {id} não encontrada");

                await _repositorio.ExcluirAsync(entidade);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no service ao excluir por ID: {ex.Message}", ex);
            }
        }

        protected virtual Task ValidarEntidade(TEntidade entidade)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing && _repositorio is IDisposable disposable)
                disposable.Dispose();

            _disposed = true;
        }
    }
}
