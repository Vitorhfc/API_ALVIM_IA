using Admin_Service.ServiceGenerico.Interface;
using Admin_Repository.RepositorioGenerico.Interface;
using System.Linq.Expressions;

namespace Admin_Service.ServiceGenerico
{
    public class ServiceGenerico<TEntidade> : IServiceGenerico<TEntidade>, IDisposable
        where TEntidade : class
    {
        #region Campos

        protected readonly IRepositorioGenerico<TEntidade> _repositorio;
        private bool _disposed;

        #endregion

        #region Construtor

        public ServiceGenerico(IRepositorioGenerico<TEntidade> repositorio)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        }

        #endregion

        #region Consultas

        public virtual async Task<IEnumerable<TEntidade?>> BuscarTodosAsync()
        {
            try
            {
                return await _repositorio.BuscarTodosAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao buscar todas as entidades: {ex.Message}", ex);
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
                throw new ApplicationException($"Erro no serviço ao buscar por filtro: {ex.Message}", ex);
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
                throw new ApplicationException($"Erro no serviço ao buscar paginado: {ex.Message}", ex);
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
                throw new ApplicationException($"Erro no serviço ao buscar por ID '{id}': {ex.Message}", ex);
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
                throw new ApplicationException($"Erro no serviço ao contar por filtro: {ex.Message}", ex);
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
                throw new ApplicationException($"Erro no serviço ao contar total: {ex.Message}", ex);
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
                throw new ApplicationException($"Erro no serviço ao buscar primeiro: {ex.Message}", ex);
            }
        }

        #endregion

        #region Operações

        public virtual async Task<TEntidade?> AdicionarAsync(TEntidade entity)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(entity, nameof(entity));
                await ValidarEntidadeAsync(entity);
                return await _repositorio.AdicionarAsync(entity);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao adicionar entidade: {ex.Message}", ex);
            }
        }

        public virtual async Task<List<TEntidade>> AdicionarArrayAsync(List<TEntidade> entities)
        {
            try
            {
                if (entities == null || entities.Count == 0)
                    throw new ArgumentException("Lista de entidades não pode ser nula ou vazia", nameof(entities));

                foreach (var entity in entities)
                    await ValidarEntidadeAsync(entity);

                return await _repositorio.AdicionarArrayAsync(entities);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao adicionar array de entidades: {ex.Message}", ex);
            }
        }

        public virtual async Task<TEntidade?> EditarAsync(TEntidade entity)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(entity, nameof(entity));
                await ValidarEntidadeAsync(entity);
                return await _repositorio.EditarAsync(entity);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao editar entidade: {ex.Message}", ex);
            }
        }

        public virtual async Task<List<TEntidade>> EditarArrayAsync(List<TEntidade> entities)
        {
            try
            {
                if (entities == null || entities.Count == 0)
                    throw new ArgumentException("Lista de entidades não pode ser nula ou vazia", nameof(entities));

                foreach (var entity in entities)
                    await ValidarEntidadeAsync(entity);

                return await _repositorio.EditarArrayAsync(entities);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao editar array de entidades: {ex.Message}", ex);
            }
        }

        public virtual async Task ExcluirAsync(TEntidade entity)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(entity, nameof(entity));
                await _repositorio.ExcluirAsync(entity);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao excluir entidade: {ex.Message}", ex);
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
                    throw new InvalidOperationException($"Entidade com ID '{id}' não encontrada");

                await _repositorio.ExcluirAsync(entidade);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao excluir por ID '{id}': {ex.Message}", ex);
            }
        }

        #endregion

        #region Validações

        public virtual Task ValidarEntidadeAsync(TEntidade entity)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Métodos Auxiliares

        protected static void ValidarCampoObrigatorio(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{fieldName} é obrigatório", fieldName);
        }

        protected static void ValidarCampoNaoNulo<T>(T value, string fieldName) where T : class
        {
            if (value == null)
                throw new ArgumentException($"{fieldName} não pode ser nulo", fieldName);
        }

        #endregion

        #region Dispose

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

        #endregion
    }
}