using Shared.Classes.ModelView.Client;

namespace Client_Service.Service.Interface
{
    /// <summary>
    /// Serviço para operações de atendimento via WhatsApp
    /// Coordena envio de mensagens, reações e controle de modo de resposta
    /// </summary>
    public interface IAtendimentoWhatsAppService
    {
        /// <summary>
        /// Envia mensagem de texto para um cliente
        /// </summary>
        Task<AtendimentoWhatsAppResponse> EnviarMensagemTextoAsync(
            EnviarMensagemTextoRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Envia mensagem com mídia para um cliente
        /// </summary>
        Task<AtendimentoWhatsAppResponse> EnviarMensagemMidiaAsync(
            EnviarMensagemMidiaRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Envia áudio/voice note para um cliente
        /// </summary>
        Task<AtendimentoWhatsAppResponse> EnviarAudioAsync(
            EnviarAudioRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reage a uma mensagem com emoji
        /// </summary>
        Task<AtendimentoWhatsAppResponse> ReagirMensagemAsync(
            ReagirMensagemRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove uma mensagem enviada
        /// </summary>
        Task<AtendimentoWhatsAppResponse> RemoverMensagemAsync(
            RemoverMensagemRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Edita uma mensagem enviada
        /// </summary>
        Task<AtendimentoWhatsAppResponse> EditarMensagemAsync(
            EditarMensagemRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Alterna modo de resposta entre IA e atendente humano
        /// </summary>
        Task<AtendimentoWhatsAppResponse> AlternarModoRespostaAsync(
            AlternarModoRespostaRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém o status atual do modo de resposta de um cliente
        /// </summary>
        Task<StatusModoRespostaResponse?> ObterStatusModoRespostaAsync(
            string clienteId,
            CancellationToken cancellationToken = default);
    }
}
