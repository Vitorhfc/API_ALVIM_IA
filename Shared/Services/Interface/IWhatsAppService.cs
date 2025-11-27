using Shared.Classes.Model;
using System.IO;
using System.Threading;

namespace Shared.Services.Interface
{
    /// <summary>
    /// Interface para serviço genérico de envio de mensagens WhatsApp via WAHA API
    /// </summary>
    public interface IWhatsAppService
    {
        #region Envio de Mensagens

        /// <summary>
        /// Envia mensagem WhatsApp (texto ou mídia)
        /// </summary>
        Task<EnvioResponse> EnviarMensagemAsync(
            EnviarWhatsAppRequest request,
            string wahaApiUrl,
            string wahaApiKey);

        /// <summary>
        /// Envia mensagem de texto simples
        /// </summary>
        Task<EnvioResponse> EnviarTextoAsync(
            string numeroDestino,
            string mensagem,
            string session,
            string wahaApiUrl,
            string wahaApiKey);

        /// <summary>
        /// Envia mensagem com mídia (imagem, vídeo, documento)
        /// </summary>
        Task<EnvioResponse> EnviarMidiaAsync(
            string numeroDestino,
            string mensagem,
            string urlMidia,
            string session,
            TipoMensagemWhatsApp tipo,
            string wahaApiUrl,
            string wahaApiKey);

        /// <summary>
        /// Envia áudio/voice note
        /// </summary>
        Task<EnvioResponse> EnviarAudioAsync(
            string numeroDestino,
            string urlAudio,
            string session,
            string wahaApiUrl,
            string wahaApiKey,
            string? quotedMessageId = null);

        #endregion

        #region Gerenciamento de Sessões

        /// <summary>
        /// Cria ou atualiza uma sessão WAHA com configuração completa
        /// PUT /api/sessions/{name}
        /// </summary>
        Task<CreateWahaSessionResponse?> UpsertSessionAsync(
            CreateWahaSessionRequest request,
            string wahaApiUrl,
            string wahaApiKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inicia uma sessão WAHA existente
        /// POST /api/sessions/{name}/start
        /// </summary>
        Task<CreateWahaSessionResponse?> StartSessionAsync(
            string sessionName,
            string wahaApiUrl,
            string wahaApiKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cria uma nova sessão WAHA completa (Upsert + Start)
        /// </summary>
        Task<CreateWahaSessionResponse?> CreateSessionAsync(
            CreateWahaSessionRequest request,
            string wahaApiUrl,
            string wahaApiKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reinicia uma sessão WAHA existente
        /// POST /api/sessions/{name}/restart
        /// </summary>
        Task<bool> RestartSessionAsync(
            string instanceName,
            string wahaApiUrl,
            string wahaApiKey);

        /// <summary>
        /// Lista todas as sessões disponíveis na API WAHA
        /// GET /api/sessions?all={true|false}
        /// </summary>
        Task<List<WAHASessionInfo>> ListarSessionsAsync(
            string wahaApiUrl,
            string wahaApiKey,
            bool incluirTodas = true);

        #endregion

        #region Status e QR Code

        /// <summary>
        /// Obtém status de uma instância WAHA
        /// GET /api/sessions/{name}
        /// </summary>
        Task<WAHAStatusResponse> GetInstanceStatusAsync(
            string instanceName,
            string wahaApiUrl,
            string wahaApiKey);

        /// <summary>
        /// Obtém QR Code para autenticação
        /// GET /api/{name}/auth/qr?format=image
        /// </summary>
        Task<WAHAQrCodeResponse> GetQrCodeAsync(
            string instanceName,
            string wahaApiUrl,
            string wahaApiKey);

        #endregion

        #region Webhook

        /// <summary>
        /// Configura webhook para receber eventos da instância
        /// POST /api/{name}/settings/webhooks
        /// </summary>
        Task<bool> SetWebhookAsync(
            string instanceName,
            string webhookUrl,
            string wahaApiUrl,
            string wahaApiKey);

        #endregion

        #region Reações

        /// <summary>
        /// Envia reação (emoji) para uma mensagem
        /// PUT /api/reaction
        /// </summary>
        Task<EnvioResponse> EnviarReacaoAsync(
            string idMensagem,
            string emoji,
            string session,
            string wahaApiUrl,
            string wahaApiKey);

        #endregion

        #region Validação e Verificação

        /// <summary>
        /// Valida se um número existe no WhatsApp usando WAHA API
        /// GET /api/contacts/check-exists?phone={phone}&session={session}
        /// </summary>
        Task<FetchNumberIdResult> FetchNumberIdAsync(
            string instanceName,
            string phone,
            string apiUrl,
            string apiKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém URL da foto de perfil
        /// GET /api/contacts/profile-picture?contactId={contactId}&refresh={bool}&session={session}
        /// </summary>
        Task<string?> GetProfileImageUrlAsync(
            string instanceName,
            string phoneOrId,
            string apiUrl,
            string apiKey,
            CancellationToken cancellationToken = default);

        #endregion

        #region Gerenciamento de Mensagens

        /// <summary>
        /// Remove uma mensagem para todos os participantes
        /// DELETE /api/{instance}/chats/{chatId}/messages/{messageId}
        /// </summary>
        Task<bool> RemoveMessageForAllAsync(
            string instanceName,
            string phoneOrId,
            string messageId,
            string apiUrl,
            string apiKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Edita uma mensagem enviada
        /// PUT /api/{instance}/chats/{chatId}/messages/{messageId}
        /// </summary>
        Task<bool> EditMessageAsync(
            string instanceName,
            string phoneOrId,
            string messageId,
            string editedMessage,
            string apiUrl,
            string apiKey,
            CancellationToken cancellationToken = default);

        #endregion

        #region Consulta de Contatos

        /// <summary>
        /// Consulta informações do contato na API do WAHA
        /// GET /api/contacts?contactId={contactId}&session={session}
        /// </summary>
        Task<WahaContactInfo?> ObterInformacoesContatoAsync(
            string contactId,
            string session,
            string wahaApiUrl,
            string wahaApiKey,
            CancellationToken cancellationToken = default);

        #endregion

        #region Download de Mídia

        /// <summary>
        /// Baixa mídia do WhatsApp para um arquivo local
        /// GET /api/{instance}/files/{keyId}
        /// </summary>
        Task<Stream?> DownloadMediaToDiskAsync(
            string destinationFilePath,
            string instanceName,
            string keyId,
            string apiUrl,
            string apiKey,
            CancellationToken cancellationToken = default,
            string? downloadUrl = null);

        #endregion
    }
}