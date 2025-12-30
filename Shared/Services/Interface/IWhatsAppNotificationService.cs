namespace Shared.Services.Interface
{
    /// <summary>
    /// Interface para serviço de notificações WhatsApp via SignalR
    /// Notifica todas as conexões de uma empresa
    /// </summary>
    public interface IWhatsAppNotificationService
    {
        /// <summary>
        /// Notifica todas as conexões de uma empresa sobre uma nova mensagem recebida
        /// </summary>
        /// <param name="empresaId">ID da empresa que receberá a notificação</param>
        /// <param name="eventType">Tipo do evento (ex: message.any, status.update)</param>
        /// <param name="payload">Dados do payload do webhook</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task NotificarNovaMensagemAsync(
            string empresaId,
            string eventType,
            object payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifica todas as conexões de uma empresa sobre uma atualização de status
        /// </summary>
        /// <param name="empresaId">ID da empresa</param>
        /// <param name="sessionName">Nome da sessão</param>
        /// <param name="status">Status atual</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task NotificarStatusSessaoAsync(
            string empresaId,
            string sessionName,
            string status,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se há conexões ativas para uma empresa
        /// </summary>
        /// <param name="empresaId">ID da empresa</param>
        /// <returns>True se houver conexões ativas</returns>
        bool TemConexoesAtivas(string empresaId);

        /// <summary>
        /// Obtém o número de conexões ativas para uma empresa
        /// </summary>
        /// <param name="empresaId">ID da empresa</param>
        /// <returns>Número de conexões ativas</returns>
        int ObterNumeroConexoes(string empresaId);

        // ===== Métodos de compatibilidade (detectam empresa automaticamente) =====

        /// <summary>
        /// Notifica mudança de status geral do WhatsApp (detecta empresa pela sessão)
        /// </summary>
        /// <param name="sessionName">Nome da sessão (formato: NomeEmpresa_EmpresaId_...)</param>
        /// <param name="status">Status atual (WORKING, SCAN_QR_CODE, STARTING, FAILED, STOPPED)</param>
        /// <param name="message">Mensagem descritiva</param>
        /// <param name="telefone">Número de telefone conectado (opcional)</param>
        Task NotificarStatusAlteradoAsync(string sessionName, string status, string? message = null, string? telefone = null);

        /// <summary>
        /// Notifica que um QR Code foi gerado (detecta empresa pela sessão)
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <param name="qrCode">String do QR Code</param>
        Task NotificarQRCodeGeradoAsync(string sessionName, string qrCode);

        /// <summary>
        /// Notifica que o WhatsApp foi conectado com sucesso (detecta empresa pela sessão)
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <param name="telefone">Número de telefone conectado</param>
        Task NotificarWhatsAppConectadoAsync(string sessionName, string telefone);

        /// <summary>
        /// Notifica que o WhatsApp foi desconectado (detecta empresa pela sessão)
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <param name="motivo">Motivo da desconexão (opcional)</param>
        Task NotificarWhatsAppDesconectadoAsync(string sessionName, string? motivo = null);

        /// <summary>
        /// Notifica todas as conexões de uma empresa sobre atualização de um cliente
        /// </summary>
        /// <param name="empresaId">ID da empresa</param>
        /// <param name="tipoAtualizacao">Tipo de atualização (criado, atualizado, status_alterado, etc.)</param>
        /// <param name="cliente">Dados do cliente</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task NotificarClienteAtualizadoAsync(
            string empresaId,
            string tipoAtualizacao,
            object cliente,
            CancellationToken cancellationToken = default);
    }
}
