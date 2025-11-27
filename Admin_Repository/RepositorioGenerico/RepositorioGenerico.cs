using Admin_Repository.Configuration;
using Admin_Repository.RepositorioGenerico.Interface;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shared.Classes.Model;
using System.Linq.Expressions;

namespace Admin_Repository.RepositorioGenerico
{
    /// <summary>
    /// Implementação genérica de repositório para operações no banco administrativo
    /// </summary>
    /// <typeparam name="TEntidade">Tipo da entidade</typeparam>
    public class RepositorioGenerico<TEntidade> : IRepositorioGenerico<TEntidade>, IDisposable
        where TEntidade : class
    {
        #region Campos

        private readonly ContextBaseAdmin _contextBaseAdmin;
        private readonly string _nomeColecao;
        private bool _disposed;

        protected IMongoCollection<TEntidade> _collection => ObterCollectionAtual();

        #endregion

        #region Construtor

        public RepositorioGenerico(ContextBaseAdmin contextBaseAdmin, string nomeColecao)
        {
            _contextBaseAdmin = contextBaseAdmin ?? throw new ArgumentNullException(nameof(contextBaseAdmin));
            _nomeColecao = typeof(TEntidade).Name;
        }

        #endregion

        #region Consultas

        public async Task<IEnumerable<TEntidade?>> BuscarTodosAsync()
        {
            try
            {
                return await _collection.Find(Builders<TEntidade>.Filter.Empty).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar todas as entidades da coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<TEntidade?>> BuscarPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(expression, nameof(expression));
                return await _collection.Find(expression).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar com filtro na coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<TEntidade?>> BuscarPorFiltroPaginadoAsync(
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

                return await _collection.Find(expression)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar paginado na coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }

        public async Task<TEntidade?> BuscarPorIdAsync(string? id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    throw new ArgumentNullException(nameof(id));

                var filter = Builders<TEntidade>.Filter.Eq("Id", id);
                return await _collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar por ID '{id}' na coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }

        public async Task<int> BuscarContagemTotalPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(expression, nameof(expression));
                return (int)await _collection.CountDocumentsAsync(expression);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao contar documentos na coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }

        public async Task<int> BuscarContagemTotalAsync()
        {
            try
            {
                return (int)await _collection.CountDocumentsAsync(Builders<TEntidade>.Filter.Empty);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao contar total na coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }

        public async Task<TEntidade?> BuscarPrimeiroPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(expression, nameof(expression));
                return await _collection.Find(expression).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar primeiro na coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }
        #endregion

        #region Operações

        public async Task<TEntidade?> AdicionarAsync(TEntidade entity)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(entity, nameof(entity));
                await _collection.InsertOneAsync(entity);
                return entity;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao adicionar na coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }

        public async Task<List<TEntidade>> AdicionarArrayAsync(List<TEntidade> entities)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(entities, nameof(entities));

                if (entities.Count == 0)
                    return entities;

                await _collection.InsertManyAsync(entities);
                return entities;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao adicionar array na coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }

        public async Task<TEntidade?> EditarAsync(TEntidade entity)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(entity, nameof(entity));

                var idProperty = typeof(TEntidade).GetProperty("Id")
                    ?? throw new InvalidOperationException("Propriedade 'Id' não encontrada na entidade");

                var id = idProperty.GetValue(entity)?.ToString();
                if (string.IsNullOrEmpty(id))
                    throw new InvalidOperationException("ID não pode ser nulo");

                var filter = Builders<TEntidade>.Filter.Eq("Id", id);
                var entidadeAtual = await _collection.Find(filter).FirstOrDefaultAsync();

                if (entidadeAtual == null)
                    throw new KeyNotFoundException($"Entidade com ID '{id}' não encontrada na coleção {_nomeColecao}");

                var updates = new List<UpdateDefinition<TEntidade>>();

                foreach (var prop in typeof(TEntidade).GetProperties())
                {
                    if (prop.Name == "Id" || !prop.CanRead)
                        continue;

                    var novoValor = prop.GetValue(entity);
                    var valorAtual = prop.GetValue(entidadeAtual);

                    if (novoValor != null && !Equals(novoValor, valorAtual))
                        updates.Add(Builders<TEntidade>.Update.Set(prop.Name, novoValor));
                }

                if (updates.Count == 0)
                    return entidadeAtual;

                var result = await _collection.UpdateOneAsync(filter, Builders<TEntidade>.Update.Combine(updates));
                return result.ModifiedCount > 0 ? entity : entidadeAtual;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao editar na coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }

        public async Task<List<TEntidade>> EditarArrayAsync(List<TEntidade> entities)
        {
            try
            {
                if (entities == null || entities.Count == 0)
                    throw new ArgumentException("Lista de entidades não pode ser nula ou vazia");

                var idProperty = typeof(TEntidade).GetProperty("Id")
                    ?? throw new InvalidOperationException("Propriedade 'Id' não encontrada na entidade");

                var tasks = entities.Select(async entity =>
                {
                    var id = idProperty.GetValue(entity)?.ToString();
                    if (string.IsNullOrEmpty(id))
                        throw new InvalidOperationException("ID não pode ser nulo");

                    var filter = Builders<TEntidade>.Filter.Eq("Id", id);
                    var result = await _collection.ReplaceOneAsync(filter, entity);

                    if (result.ModifiedCount == 0 && result.MatchedCount == 0)
                        throw new KeyNotFoundException($"Entidade com ID '{id}' não encontrada na coleção {_nomeColecao}");

                    return entity;
                });

                return (await Task.WhenAll(tasks)).ToList();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao editar array na coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }

        public async Task ExcluirAsync(TEntidade entity)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(entity, nameof(entity));

                var idProperty = typeof(TEntidade).GetProperty("Id")
                    ?? throw new InvalidOperationException("Propriedade 'Id' não encontrada na entidade");

                var id = idProperty.GetValue(entity)?.ToString();
                if (string.IsNullOrEmpty(id))
                    throw new InvalidOperationException("ID não pode ser nulo");

                var filter = Builders<TEntidade>.Filter.Eq("Id", id);
                var result = await _collection.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                    throw new KeyNotFoundException($"Entidade com ID '{id}' não encontrada na coleção {_nomeColecao}");
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao excluir na coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }

        public async Task ExcluirPorIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    throw new ArgumentNullException(nameof(id));

                var filter = Builders<TEntidade>.Filter.Eq("Id", id);
                var result = await _collection.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                    throw new KeyNotFoundException($"Entidade com ID '{id}' não encontrada na coleção {_nomeColecao}");
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao excluir por ID '{id}' na coleção {_nomeColecao}: {ex.Message}", ex);
            }
        }

        #endregion

        #region Métodos Auxiliares

        protected IMongoCollection<TEntidade> ObterCollectionAtual()
        {
            return _contextBaseAdmin.GetCollection<TEntidade>(_nomeColecao);
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

            _disposed = true;
        }

        #endregion
    }
}
