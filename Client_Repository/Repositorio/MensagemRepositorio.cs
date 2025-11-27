
using Client_Repository.Configuration.Contexto.Interface;
using Client_Repository.Repositorio.Interface;
using Client_Repository.RepositorioGenerico;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Shared.Classes.Entidades.Client;

namespace Client_Repository.Repositorio
{
    public class MensagemRepositorio : RepositorioGenerico<Mensagem>, IMensagemRepositorio
    {
        private readonly IContextoMultiTenantService _contextoMultiTenant;

        public MensagemRepositorio(
            IContextoMultiTenantService contextoMultiTenant,
            IHttpContextAccessor httpContextAccessor
        ) : base(contextoMultiTenant, httpContextAccessor, "Mensagens")
        {
            _contextoMultiTenant = contextoMultiTenant;
        }

        public async Task<IEnumerable<Mensagem>> BuscarUltimasMensagensAsync(string clienteId, int quantidade)
        {
            var db = await _contextoMultiTenant.ObterBaseDadosAtualAsync();
            var colecao = db.GetCollection<Mensagem>("Mensagens");

            // Buscar últimas N mensagens ordenadas por data de recebimento (mais recente primeiro)
            var mensagens = await colecao
                .Find(m => m.ClienteId == clienteId)
                .SortByDescending(m => m.DtRecebido)
                .Limit(quantidade)
                .ToListAsync();

            // Retornar em ordem cronológica (mais antiga primeiro)
            return mensagens.OrderBy(m => m.DtRecebido).ToList();
        }

        public async Task<IEnumerable<Mensagem>> BuscarMensagensPorGrupoAsync(string grupoId)
        {
            return await BuscarPorFiltroAsync(m => m.GrupoProcessamentoId == grupoId);
        }

        /// <summary>
        /// ✅ IMPLEMENTAÇÃO: Atualiza status de entrega
        /// </summary>
        public async Task AtualizarStatusEntregaAsync(string mensagemId, StatusEntrega status)
        {
            var db = await _contextoMultiTenant.ObterBaseDadosAtualAsync();
            var colecao = db.GetCollection<Mensagem>("Mensagens");

            var update = Builders<Mensagem>.Update
                .Set(m => m.StatusEntrega, status);

            await colecao.UpdateOneAsync(m => m.Id == mensagemId, update);
        }

        /// <summary>
        /// Atualiza o status de entrega da mensagem (alias para AtualizarStatusEntregaAsync)
        /// </summary>
        public async Task AtualizarStatusAsync(string mensagemId, StatusEntrega status)
        {
            await AtualizarStatusEntregaAsync(mensagemId, status);
        }

        public async Task AdicionarReacaoAsync(string mensagemId, Reacao reacao)
        {
            var db = await _contextoMultiTenant.ObterBaseDadosAtualAsync();
            var colecao = db.GetCollection<Mensagem>("Mensagens");

            var update = Builders<Mensagem>.Update
                .Push(m => m.Reacoes, reacao);

            await colecao.UpdateOneAsync(m => m.Id == mensagemId, update);
        }

        public async Task MarcarReacaoComoEnviadaAsync(string mensagemId, string emoji)
        {
            var db = await _contextoMultiTenant.ObterBaseDadosAtualAsync();
            var colecao = db.GetCollection<Mensagem>("Mensagens");

            var filter = Builders<Mensagem>.Filter.And(
                Builders<Mensagem>.Filter.Eq(m => m.Id, mensagemId),
                Builders<Mensagem>.Filter.ElemMatch(m => m.Reacoes, r => r.Emoji == emoji)
            );

            var update = Builders<Mensagem>.Update
                .Set("Reacoes.$.FlgEnviada", true)
                .Set("Reacoes.$.DtEnvio", DateTime.UtcNow);

            await colecao.UpdateOneAsync(filter, update);
        }

        /// <summary>
        /// ✅ IMPLEMENTAÇÃO: Atualiza conteúdo da mensagem (edição)
        /// </summary>
        public async Task AtualizarConteudoAsync(string mensagemId, string novoConteudo)
        {
            var db = await _contextoMultiTenant.ObterBaseDadosAtualAsync();
            var colecao = db.GetCollection<Mensagem>("Mensagens");

            var update = Builders<Mensagem>.Update
                .Set(m => m.ConteudoTexto, novoConteudo)
                .Set("Metadados.FlgEditada", true)
                .Set("Metadados.DtEdicao", DateTime.UtcNow.ToString("o"));

            await colecao.UpdateOneAsync(m => m.Id == mensagemId, update);
        }

        /// <summary>
        /// ✅ IMPLEMENTAÇÃO: Marca mensagem como deletada
        /// </summary>
        public async Task MarcarComoDeletadaAsync(string mensagemId)
        {
            var db = await _contextoMultiTenant.ObterBaseDadosAtualAsync();
            var colecao = db.GetCollection<Mensagem>("Mensagens");

            var update = Builders<Mensagem>.Update
                .Set("Metadados.FlgDeletada", true)
                .Set("Metadados.DtDelecao", DateTime.UtcNow.ToString("o"));

            await colecao.UpdateOneAsync(m => m.Id == mensagemId, update);
        }

        /// <summary>
        /// Busca mensagem pelo ID do WhatsApp
        /// </summary>
        public async Task<Mensagem?> BuscarPorIdMensagemWhatsAppAsync(string idMensagemWhatsApp)
        {
            var db = await _contextoMultiTenant.ObterBaseDadosAtualAsync();
            var colecao = db.GetCollection<Mensagem>("Mensagens");

            var mensagem = await colecao
                .Find(m => m.IdMensagemWhatsApp == idMensagemWhatsApp)
                .FirstOrDefaultAsync();

            return mensagem;
        }

        /// <summary>
        /// Atualiza conteúdo da mensagem por ID do WhatsApp
        /// </summary>
        public async Task AtualizarConteudoPorIdWhatsAppAsync(string idMensagemWhatsApp, string novoConteudo)
        {
            var db = await _contextoMultiTenant.ObterBaseDadosAtualAsync();
            var colecao = db.GetCollection<Mensagem>("Mensagens");

            var update = Builders<Mensagem>.Update
                .Set(m => m.ConteudoTexto, novoConteudo)
                .Set("Metadados.FlgEditada", true)
                .Set("Metadados.DtEdicao", DateTime.UtcNow.ToString("o"));

            await colecao.UpdateOneAsync(m => m.IdMensagemWhatsApp == idMensagemWhatsApp, update);
        }

        /// <summary>
        /// Marca mensagem como deletada por ID do WhatsApp
        /// </summary>
        public async Task MarcarComoDeletadaPorIdWhatsAppAsync(string idMensagemWhatsApp)
        {
            var db = await _contextoMultiTenant.ObterBaseDadosAtualAsync();
            var colecao = db.GetCollection<Mensagem>("Mensagens");

            var update = Builders<Mensagem>.Update
                .Set("Metadados.FlgDeletada", true)
                .Set("Metadados.DtDelecao", DateTime.UtcNow.ToString("o"));

            await colecao.UpdateOneAsync(m => m.IdMensagemWhatsApp == idMensagemWhatsApp, update);
        }
    }
}