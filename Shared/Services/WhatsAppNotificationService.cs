using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shared.Hubs;
using Shared.Services.Interface;

namespace Shared.Services
{
    /// <summary>
    /// Serviço para enviar notificações em tempo real via SignalR sobre o status do WhatsApp
    /// </summary>
    public class WhatsAppNotificationService : IWhatsAppNotificationService
    {
        private readonly IHubContext<WhatsAppHub> _hubContext;
        private readonly ILogger<WhatsAppNotificationService> _logger;

        public WhatsAppNotificationService(
            IHubContext<WhatsAppHub> hubContext,
            ILogger<WhatsAppNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Notifica mudança de status geral
        /// </summary>
        public async Task NotificarStatusAlteradoAsync(
            string sessionName,
            string status,
            string? message = null,
            string? telefone = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                {
                    _logger.LogWarning("Tentativa de notificação com sessionName vazio");
                    return;
                }

                // Verifica se há clientes conectados para esta sessão
                if (!WhatsAppHub.TemConexoesAtivas(sessionName))
                {
                    _logger.LogDebug(
                        "Nenhum cliente conectado para a sessão {SessionName}. Notificação não enviada.",
                        sessionName
                    );
                    return;
                }

                var update = new
                {
                    sessionName,
                    status,
                    message,
                    telefone,
                    timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients
                    .Group(sessionName)
                    .SendAsync("WhatsAppStatusChanged", update);

                _logger.LogInformation(
                    "Notificação enviada via SignalR: {SessionName} - {Status} - {NumClientes} cliente(s)",
                    sessionName, status, WhatsAppHub.ObterNumeroConexoesPorSessao(sessionName)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao enviar notificação de status via SignalR - SessionName: {SessionName}, Status: {Status}",
                    sessionName, status
                );
            }
        }

        /// <summary>
        /// Notifica que um QR Code foi gerado
        /// </summary>
        public async Task NotificarQRCodeGeradoAsync(string sessionName, string qrCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName) || string.IsNullOrWhiteSpace(qrCode))
                {
                    _logger.LogWarning("Tentativa de notificação de QR Code com parâmetros inválidos");
                    return;
                }

                // Verifica se há clientes conectados
                if (!WhatsAppHub.TemConexoesAtivas(sessionName))
                {
                    _logger.LogDebug(
                        "Nenhum cliente conectado para a sessão {SessionName}. QR Code não enviado via SignalR.",
                        sessionName
                    );
                    return;
                }

                var data = new
                {
                    sessionName,
                    qrCode
                };

                await _hubContext.Clients
                    .Group(sessionName)
                    .SendAsync("QRCodeGerado", data);

                _logger.LogInformation(
                    "QR Code enviado via SignalR para {SessionName} - {NumClientes} cliente(s)",
                    sessionName, WhatsAppHub.ObterNumeroConexoesPorSessao(sessionName)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao enviar QR Code via SignalR - SessionName: {SessionName}",
                    sessionName
                );
            }
        }

        /// <summary>
        /// Notifica que o WhatsApp foi conectado com sucesso
        /// </summary>
        public async Task NotificarWhatsAppConectadoAsync(string sessionName, string telefone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                {
                    _logger.LogWarning("Tentativa de notificação de conexão com sessionName vazio");
                    return;
                }

                // Verifica se há clientes conectados
                if (!WhatsAppHub.TemConexoesAtivas(sessionName))
                {
                    _logger.LogDebug(
                        "Nenhum cliente conectado para a sessão {SessionName}. Notificação de conexão não enviada.",
                        sessionName
                    );
                    return;
                }

                var data = new
                {
                    sessionName,
                    telefone
                };

                await _hubContext.Clients
                    .Group(sessionName)
                    .SendAsync("WhatsAppConectado", data);

                _logger.LogInformation(
                    "Notificação de WhatsApp conectado enviada via SignalR: {SessionName} - {Telefone} - {NumClientes} cliente(s)",
                    sessionName, telefone, WhatsAppHub.ObterNumeroConexoesPorSessao(sessionName)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao enviar notificação de WhatsApp conectado via SignalR - SessionName: {SessionName}",
                    sessionName
                );
            }
        }

        /// <summary>
        /// Notifica que o WhatsApp foi desconectado
        /// </summary>
        public async Task NotificarWhatsAppDesconectadoAsync(string sessionName, string? motivo = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                {
                    _logger.LogWarning("Tentativa de notificação de desconexão com sessionName vazio");
                    return;
                }

                // Verifica se há clientes conectados
                if (!WhatsAppHub.TemConexoesAtivas(sessionName))
                {
                    _logger.LogDebug(
                        "Nenhum cliente conectado para a sessão {SessionName}. Notificação de desconexão não enviada.",
                        sessionName
                    );
                    return;
                }

                var data = new
                {
                    sessionName,
                    motivo
                };

                await _hubContext.Clients
                    .Group(sessionName)
                    .SendAsync("WhatsAppDesconectado", data);

                _logger.LogInformation(
                    "Notificação de WhatsApp desconectado enviada via SignalR: {SessionName} - {Motivo} - {NumClientes} cliente(s)",
                    sessionName, motivo ?? "Não especificado", WhatsAppHub.ObterNumeroConexoesPorSessao(sessionName)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao enviar notificação de WhatsApp desconectado via SignalR - SessionName: {SessionName}",
                    sessionName
                );
            }
        }
    }
}
