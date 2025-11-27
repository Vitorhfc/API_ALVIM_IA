using Client_Repository.Configuration.Contexto.Interface;
using Client_Repository.Repositorio.Interface;
using Client_Repository.RepositorioGenerico;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Shared.Classes.Entidades.Client;

namespace Client_Repository.Repositorio
{
    public class PlanoContextoRepositorio : RepositorioGenerico<PlanoContexto>, IPlanoContextoRepositorio
    {
        public PlanoContextoRepositorio(
            IContextoMultiTenantService contextoMultiTenant,
            IHttpContextAccessor httpContextAccessor)
            : base(contextoMultiTenant, httpContextAccessor, "PlanoContexto")
        {
        }

        public async Task<List<PlanoContexto>> BuscarTodosAsync()
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(_ => true)
                .SortBy(p => p.Ordem)
                .ToListAsync();
        }

        public async Task<List<PlanoContexto>> BuscarAtivosAsync()
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(p => p.FlgAtivo)
                .SortBy(p => p.Ordem)
                .ToListAsync();
        }

        public async Task<PlanoContexto?> BuscarPorIdAsync(string id)
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<PlanoContexto>> BuscarPorTipoAsync(TipoPlanoContexto tipo)
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(p => p.Tipo == tipo).ToListAsync();
        }

        public async Task<PlanoContexto?> BuscarPlanoBaseAsync()
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(p => p.Tipo == TipoPlanoContexto.PlanoBase)
                .FirstOrDefaultAsync();
        }

        public async Task<PlanoContexto> CriarAsync(PlanoContexto plano)
        {
            var collection = await ObterColecaoAsync();
            plano.DtCriacao = DateTime.UtcNow;
            plano.DtAlteracao = DateTime.UtcNow;

            // Se for plano base, garantir que está ativo
            if (plano.Tipo == TipoPlanoContexto.PlanoBase)
            {
                plano.FlgAtivo = true;
                plano.FlgPadrao = true;
            }

            await collection.InsertOneAsync(plano);
            return plano;
        }

        public async Task<PlanoContexto> AtualizarAsync(PlanoContexto plano)
        {
            var collection = await ObterColecaoAsync();
            plano.DtAlteracao = DateTime.UtcNow;

            // Se for plano base, garantir que está ativo
            if (plano.Tipo == TipoPlanoContexto.PlanoBase)
            {
                plano.FlgAtivo = true;
            }

            await collection.ReplaceOneAsync(p => p.Id == plano.Id, plano);
            return plano;
        }

        public async Task<bool> DeletarAsync(string id)
        {
            var collection = await ObterColecaoAsync();

            // Verificar se pode deletar (não é padrão)
            var plano = await BuscarPorIdAsync(id);
            if (plano == null || plano.FlgPadrao)
            {
                return false;
            }

            var result = await collection.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> AlterarStatusAsync(string id, bool flgAtivo)
        {
            var collection = await ObterColecaoAsync();

            // Verificar se é plano base (não pode desativar)
            var plano = await BuscarPorIdAsync(id);
            if (plano == null)
            {
                return false;
            }

            // Plano base sempre ativo
            if (plano.Tipo == TipoPlanoContexto.PlanoBase)
            {
                flgAtivo = true;
            }

            var update = Builders<PlanoContexto>.Update
                .Set(p => p.FlgAtivo, flgAtivo)
                .Set(p => p.DtAlteracao, DateTime.UtcNow);

            var result = await collection.UpdateOneAsync(p => p.Id == id, update);
            return result.ModifiedCount > 0;
        }

        public async Task<int> AtualizarVariosAsync(List<PlanoContexto> planos)
        {
            var collection = await ObterColecaoAsync();
            var bulkOps = new List<WriteModel<PlanoContexto>>();

            foreach (var plano in planos)
            {
                plano.DtAlteracao = DateTime.UtcNow;

                // Plano base sempre ativo
                if (plano.Tipo == TipoPlanoContexto.PlanoBase)
                {
                    plano.FlgAtivo = true;
                }

                var filter = Builders<PlanoContexto>.Filter.Eq(p => p.Id, plano.Id);
                var replaceOne = new ReplaceOneModel<PlanoContexto>(filter, plano);
                bulkOps.Add(replaceOne);
            }

            if (bulkOps.Count == 0)
            {
                return 0;
            }

            var result = await collection.BulkWriteAsync(bulkOps);
            return (int)result.ModifiedCount;
        }

        public async Task<bool> ExisteTipoAsync(TipoPlanoContexto tipo)
        {
            var collection = await ObterColecaoAsync();
            var count = await collection.CountDocumentsAsync(p => p.Tipo == tipo);
            return count > 0;
        }
    }
}
