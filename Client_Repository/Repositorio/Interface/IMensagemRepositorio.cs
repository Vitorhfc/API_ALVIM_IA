// ============================================================================
// ARQUIVO 1: IMensagemRepositorio.cs
// Localização: Client_Repository/Repositorio/Interface/IMensagemRepositorio.cs
// ============================================================================

using Client_Repository.RepositorioGenerico.Interface;
using Shared.Classes.Entidades.Client;

namespace Client_Repository.Repositorio.Interface
{
    public interface IMensagemRepositorio : IRepositorioGenerico<Mensagem>
    {
        /// <summary>
        /// Busca as últimas N mensagens de um cliente
        /// </summary>
        Task<IEnumerable<Mensagem>> BuscarUltimasMensagensAsync(string clienteId, int quantidade);

        /// <summary>
        /// Busca mensagens por grupo de processamento
        /// </summary>
        Task<IEnumerable<Mensagem>> BuscarMensagensPorGrupoAsync(string grupoId);

        /// <summary>
        /// ✅ ADICIONAR: Atualiza o status de entrega da mensagem
        /// </summary>
        Task AtualizarStatusEntregaAsync(string mensagemId, StatusEntrega status);

        /// <summary>
        /// Atualiza o status de entrega da mensagem (alias para AtualizarStatusEntregaAsync)
        /// </summary>
        Task AtualizarStatusAsync(string mensagemId, StatusEntrega status);

        /// <summary>
        /// Adiciona uma reação à mensagem
        /// </summary>
        Task AdicionarReacaoAsync(string mensagemId, Reacao reacao);

        /// <summary>
        /// Marca reação como enviada
        /// </summary>
        Task MarcarReacaoComoEnviadaAsync(string mensagemId, string emoji);

        /// <summary>
        /// ✅ ADICIONAR: Atualiza o conteúdo de uma mensagem (edição) por ID do WhatsApp
        /// </summary>
        Task AtualizarConteudoPorIdWhatsAppAsync(string idMensagemWhatsApp, string novoConteudo);

        /// <summary>
        /// ✅ ADICIONAR: Marca mensagem como deletada por ID do WhatsApp
        /// </summary>
        Task MarcarComoDeletadaPorIdWhatsAppAsync(string idMensagemWhatsApp);

        /// <summary>
        /// Busca mensagem pelo ID do WhatsApp
        /// </summary>
        Task<Mensagem?> BuscarPorIdMensagemWhatsAppAsync(string idMensagemWhatsApp);
    }
}