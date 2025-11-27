using Client_Repository.Configuration.Contexto.Interface;
using Client_Repository.RepositorioGenerico.Interface;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace Client_Repository.RepositorioGenerico
{
    public class RepositorioGenerico<TEntidade> : IRepositorioGenerico<TEntidade> where TEntidade : class
    {
        protected readonly IContextoMultiTenantService _contextoMultiTenant;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly string _nomeColecao;
        private IMongoCollection<TEntidade> _collection;

        public RepositorioGenerico(
            IContextoMultiTenantService contextoMultiTenant,
            IHttpContextAccessor httpContextAccessor,
            string nomeColecao)
        {
            _contextoMultiTenant = contextoMultiTenant ?? throw new ArgumentNullException(nameof(contextoMultiTenant));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _nomeColecao = nomeColecao ?? typeof(TEntidade).Name;
        }

        protected async Task<IMongoCollection<TEntidade>> ObterColecaoAsync()
        {
            if (_collection == null)
            {
                var database = await _contextoMultiTenant.ObterBaseDadosAtualAsync();
                _collection = database.GetCollection<TEntidade>(_nomeColecao);
            }
            return _collection;
        }

        public async Task<TEntidade> AdicionarAsync(TEntidade entidade)
        {
            var collection = await ObterColecaoAsync();
            await collection.InsertOneAsync(entidade);
            return entidade;
        }

        public async Task<List<TEntidade>> AdicionarVariosAsync(IEnumerable<TEntidade> entidades)
        {
            var collection = await ObterColecaoAsync();
            var lista = entidades.ToList();
            if (lista.Any())
            {
                await collection.InsertManyAsync(lista);
            }
            return lista;
        }

        public async Task<List<TEntidade>> AdicionarArrayAsync(List<TEntidade> array)
        {
            return await AdicionarVariosAsync(array);
        }

        public async Task<TEntidade> AtualizarAsync(TEntidade entidade)
        {
            var collection = await ObterColecaoAsync();
            var idProperty = typeof(TEntidade).GetProperty("Id");
            if (idProperty == null)
                throw new InvalidOperationException($"Entidade {typeof(TEntidade).Name} não possui propriedade 'Id'");

            var id = idProperty.GetValue(entidade);
            if (id == null)
                throw new InvalidOperationException($"O ID da entidade {typeof(TEntidade).Name} não pode ser nulo");

            // Corrigir o filtro para lidar com diferentes tipos de ID
            FilterDefinition<TEntidade> filter;

            // Se o ID é string, tentar converter para ObjectId
            if (id is string idString)
            {
                if (ObjectId.TryParse(idString, out ObjectId objectId))
                {
                    filter = Builders<TEntidade>.Filter.Eq("_id", objectId);
                }
                else
                {
                    filter = Builders<TEntidade>.Filter.Eq("_id", idString);
                }
            }
            // Se já é ObjectId
            else if (id is ObjectId objectId)
            {
                filter = Builders<TEntidade>.Filter.Eq("_id", objectId);
            }
            // Para outros tipos (int, guid, etc)
            else
            {
                filter = Builders<TEntidade>.Filter.Eq("_id", id);
            }

            var result = await collection.ReplaceOneAsync(filter, entidade);

            if (result.MatchedCount == 0)
                throw new InvalidOperationException($"Nenhuma entidade encontrada com o ID: {id}");

            return entidade;
        }


        public async Task<TEntidade> EditarAsync(TEntidade objeto)
        {
            return await AtualizarAsync(objeto);
        }

        public async Task<List<TEntidade>> EditarArrayAsync(List<TEntidade> array)
        {
            var collection = await ObterColecaoAsync();
            var resultado = new List<TEntidade>();

            foreach (var item in array)
            {
                var atualizado = await AtualizarAsync(item);
                resultado.Add(atualizado);
            }

            return resultado;
        }

        public async Task<bool> DeletarAsync(string id)
        {
            var collection = await ObterColecaoAsync();
            var filter = Builders<TEntidade>.Filter.Eq("_id", ObjectId.Parse(id));
            var result = await collection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public async Task ExcluirAsync(TEntidade objeto)
        {
            var idProperty = typeof(TEntidade).GetProperty("Id");
            if (idProperty == null)
                throw new InvalidOperationException($"Entidade {typeof(TEntidade).Name} não possui propriedade 'Id'");

            var id = idProperty.GetValue(objeto);
            if (id == null)
                throw new InvalidOperationException($"O ID da entidade {typeof(TEntidade).Name} não pode ser nulo");

            var collection = await ObterColecaoAsync();

            FilterDefinition<TEntidade> filter;

            if (id is string idString)
            {
                if (ObjectId.TryParse(idString, out ObjectId objectId))
                {
                    filter = Builders<TEntidade>.Filter.Eq("_id", objectId);
                }
                else
                {
                    filter = Builders<TEntidade>.Filter.Eq("_id", idString);
                }
            }
            else if (id is ObjectId objectId)
            {
                filter = Builders<TEntidade>.Filter.Eq("_id", objectId);
            }
            else
            {
                filter = Builders<TEntidade>.Filter.Eq("_id", id);
            }

            var result = await collection.DeleteOneAsync(filter);

            if (result.DeletedCount == 0)
                throw new InvalidOperationException($"Nenhuma entidade encontrada com o ID: {id}");
        }

        public async Task<TEntidade> ObterPorIdAsync(string id)
        {
            var collection = await ObterColecaoAsync();
            var filter = Builders<TEntidade>.Filter.Eq("_id", ObjectId.Parse(id));
            return await collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<TEntidade> BuscarPorIdAsync(string id)
        {
            return await ObterPorIdAsync(id);
        }

        public async Task<List<TEntidade>> ObterTodosAsync()
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(_ => true).ToListAsync();
        }

        public async Task<IEnumerable<TEntidade>> BuscarTodosAsync()
        {
            return await ObterTodosAsync();
        }

        public async Task<List<TEntidade>> ObterPorFiltroAsync(Expression<Func<TEntidade, bool>> filtro)
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(filtro).ToListAsync();
        }

        public async Task<IEnumerable<TEntidade>> BuscarPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression)
        {
            var collection = await ObterColecaoAsync();
            var filtro = ConverterExpression(expression);
            return await collection.Find(filtro).ToListAsync();
        }

        public async Task<TEntidade> ObterUnicoPorFiltroAsync(Expression<Func<TEntidade, bool>> filtro)
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(filtro).FirstOrDefaultAsync();
        }

        public async Task<TEntidade> BuscarPrimeiroPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression)
        {
            var collection = await ObterColecaoAsync();
            var filtro = ConverterExpression(expression);
            return await collection.Find(filtro).FirstOrDefaultAsync();
        }

        public async Task<long> ContarAsync(Expression<Func<TEntidade, bool>> filtro = null)
        {
            var collection = await ObterColecaoAsync();
            if (filtro == null)
                return await collection.CountDocumentsAsync(_ => true);

            return await collection.CountDocumentsAsync(filtro);
        }

        public async Task<int> BuscarContagemTotalAsync()
        {
            var count = await ContarAsync();
            return (int)count;
        }

        public async Task<int> BuscarContagemTotalPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression)
        {
            var collection = await ObterColecaoAsync();
            var filtro = ConverterExpression(expression);
            var count = await collection.CountDocumentsAsync(filtro);
            return (int)count;
        }

        public async Task<bool> ExisteAsync(Expression<Func<TEntidade, bool>> filtro)
        {
            var collection = await ObterColecaoAsync();
            var count = await collection.CountDocumentsAsync(filtro, new CountOptions { Limit = 1 });
            return count > 0;
        }

        public async Task<List<TEntidade>> ObterComPaginacaoAsync(
            Expression<Func<TEntidade, bool>> filtro,
            int pagina,
            int tamanhoPagina,
            Expression<Func<TEntidade, object>> ordenarPor = null,
            bool descendente = false)
        {
            var collection = await ObterColecaoAsync();
            var query = collection.Find(filtro ?? (_ => true));

            if (ordenarPor != null)
            {
                query = descendente
                    ? query.SortByDescending(ordenarPor)
                    : query.SortBy(ordenarPor);
            }

            return await query
                .Skip((pagina - 1) * tamanhoPagina)
                .Limit(tamanhoPagina)
                .ToListAsync();
        }

        public async Task<IEnumerable<TEntidade>> BuscarPorFiltroPaginadoAsync(
            Expression<Func<TEntidade?, bool>> expression,
            int page,
            int pageSize)
        {
            var collection = await ObterColecaoAsync();
            var filtro = ConverterExpression(expression);

            return await collection.Find(filtro)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<bool> AtualizarCampoAsync(string id, string nomeCampo, object valor)
        {
            var collection = await ObterColecaoAsync();
            var filter = Builders<TEntidade>.Filter.Eq("_id", ObjectId.Parse(id));
            var update = Builders<TEntidade>.Update.Set(nomeCampo, valor);
            var result = await collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeletarVariosAsync(Expression<Func<TEntidade, bool>> filtro)
        {
            var collection = await ObterColecaoAsync();
            var result = await collection.DeleteManyAsync(filtro);
            return result.DeletedCount > 0;
        }

        public IMongoCollection<TEntidade> ObterColecao()
        {
            return _collection ?? throw new InvalidOperationException("Coleção não foi inicializada. Chame um método async primeiro.");
        }

        private Expression<Func<TEntidade, bool>> ConverterExpression(Expression<Func<TEntidade?, bool>> expression)
        {
            var parameter = Expression.Parameter(typeof(TEntidade), expression.Parameters[0].Name);
            var visitor = new NullableExpressionVisitor(parameter);
            var body = visitor.Visit(expression.Body);
            return Expression.Lambda<Func<TEntidade, bool>>(body, parameter);
        }

        private class NullableExpressionVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _parameter;

            public NullableExpressionVisitor(ParameterExpression parameter)
            {
                _parameter = parameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return _parameter;
            }
        }
    }
}