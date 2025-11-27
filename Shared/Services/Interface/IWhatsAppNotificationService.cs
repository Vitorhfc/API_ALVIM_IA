namespace Shared.Services.Interface
{
    /// <summary>
    /// Interface para serviço de notificações WhatsApp via SignalR
    /// </summary>
    public interface IWhatsAppNotificationService
    {
        /// <summary>
        /// Notifica mudança de status geral do WhatsApp
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <param name="status">Status atual (WORKING, SCAN_QR_CODE, STARTING, FAILED, STOPPED)</param>
        /// <param name="message">Mensagem descritiva</param>
        /// <param name="telefone">Número de telefone conectado (opcional)</param>
        Task NotificarStatusAlteradoAsync(string sessionName, string status, string? message = null, string? telefone = null);

        /// <summary>
        /// Notifica que um QR Code foi gerado
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <param name="qrCode">String do QR Code</param>
        Task NotificarQRCodeGeradoAsync(string sessionName, string qrCode);

        /// <summary>
        /// Notifica que o WhatsApp foi conectado com sucesso
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <param name="telefone">Número de telefone conectado</param>
        Task NotificarWhatsAppConectadoAsync(string sessionName, string telefone);

        /// <summary>
        /// Notifica que o WhatsApp foi desconectado
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <param name="motivo">Motivo da desconexão (opcional)</param>
        Task NotificarWhatsAppDesconectadoAsync(string sessionName, string? motivo = null);
    }
}
