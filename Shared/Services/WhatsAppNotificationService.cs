using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shared.Hubs;
using Shared.Services.Interface;

namespace Shared.Services
{
    /// <summary>
    /// Serviço responsável por enviar notificações em tempo real via SignalR
    /// para todas as conexões de uma empresa
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
        /// Notifica todas as conexões de uma empresa sobre uma nova mensagem recebida
        /// </summary>
        public async Task NotificarNovaMensagemAsync(
            string empresaId,
            string eventType,
            object payload,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(empresaId))
                {
                    _logger.LogWarning("Tentativa de notificar com empresaId vazio");
                    return;
                }

                var nomeGrupo = WhatsAppHub.ObterNomeGrupoEmpresa(empresaId);
                var numeroConexoes = WhatsAppHub.ObterNumeroConexoesPorEmpresa(empresaId);

                _logger.LogDebug(
                    "Enviando notificação de mensagem - EmpresaId: {EmpresaId}, EventType: {EventType}, Conexões: {NumeroConexoes}",
                    empresaId, eventType, numeroConexoes);

                if (numeroConexoes == 0)
                {
                    _logger.LogDebug(
                        "Nenhuma conexão ativa para a empresa - EmpresaId: {EmpresaId}",
                        empresaId);
                    return;
                }

                // Envia notificação para todos os clientes do grupo da empresa
                await _hubContext.Clients
                    .Group(nomeGrupo)
                    .SendAsync(
                        "ReceberMensagem",
                        new
                        {
                            empresaId,
                            eventType,
                            payload,
                            timestamp = DateTime.UtcNow
                        },
                        cancellationToken);

                _logger.LogInformation(
                    "Notificação de mensagem enviada - EmpresaId: {EmpresaId}, EventType: {EventType}, Conexões notificadas: {NumeroConexoes}",
                    empresaId, eventType, numeroConexoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao enviar notificação de mensagem - EmpresaId: {EmpresaId}, EventType: {EventType}",
                    empresaId, eventType);
            }
        }

        /// <summary>
        /// Notifica todas as conexões de uma empresa sobre uma atualização de status
        /// </summary>
        public async Task NotificarStatusSessaoAsync(
            string empresaId,
            string sessionName,
            string status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(empresaId))
                {
                    _logger.LogWarning("Tentativa de notificar status com empresaId vazio");
                    return;
                }

                var nomeGrupo = WhatsAppHub.ObterNomeGrupoEmpresa(empresaId);
                var numeroConexoes = WhatsAppHub.ObterNumeroConexoesPorEmpresa(empresaId);

                _logger.LogDebug(
                    "Enviando notificação de status - EmpresaId: {EmpresaId}, Session: {SessionName}, Status: {Status}, Conexões: {NumeroConexoes}",
                    empresaId, sessionName, status, numeroConexoes);

                if (numeroConexoes == 0)
                {
                    _logger.LogDebug(
                        "Nenhuma conexão ativa para a empresa - EmpresaId: {EmpresaId}",
                        empresaId);
                    return;
                }

                // Envia notificação para todos os clientes do grupo da empresa
                await _hubContext.Clients
                    .Group(nomeGrupo)
                    .SendAsync(
                        "ReceberStatusSessao",
                        new
                        {
                            empresaId,
                            sessionName,
                            status,
                            timestamp = DateTime.UtcNow
                        },
                        cancellationToken);

                _logger.LogInformation(
                    "Notificação de status enviada - EmpresaId: {EmpresaId}, Session: {SessionName}, Status: {Status}, Conexões notificadas: {NumeroConexoes}",
                    empresaId, sessionName, status, numeroConexoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao enviar notificação de status - EmpresaId: {EmpresaId}, Session: {SessionName}",
                    empresaId, sessionName);
            }
        }

        /// <summary>
        /// Verifica se há conexões ativas para uma empresa
        /// </summary>
        public bool TemConexoesAtivas(string empresaId)
        {
            return WhatsAppHub.TemConexoesAtivas(empresaId);
        }

        /// <summary>
        /// Obtém o número de conexões ativas para uma empresa
        /// </summary>
        public int ObterNumeroConexoes(string empresaId)
        {
            return WhatsAppHub.ObterNumeroConexoesPorEmpresa(empresaId);
        }

        // ===== Métodos de compatibilidade =====

        /// <summary>
        /// Extrai o EmpresaId do nome da sessão
        /// Formato esperado: NomeEmpresa_EmpresaId_...
        /// </summary>
        private string? ExtrairEmpresaIdDaSessao(string sessionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                    return null;

                // Formato: NomeEmpresa_EmpresaId_Data
                var partes = sessionName.Split('_');
                if (partes.Length >= 2)
                {
                    return partes[1]; // EmpresaId está na segunda posição
                }

                _logger.LogWarning("Formato de sessionName inválido: {SessionName}", sessionName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao extrair EmpresaId da sessão: {SessionName}", sessionName);
                return null;
            }
        }

        /// <summary>
        /// Notifica mudança de status geral (compatibilidade - detecta empresa pela sessão)
        /// </summary>
        public async Task NotificarStatusAlteradoAsync(
            string sessionName,
            string status,
            string? message = null,
            string? telefone = null)
        {
            var empresaId = ExtrairEmpresaIdDaSessao(sessionName);
            if (empresaId == null)
            {
                _logger.LogWarning("Não foi possível extrair EmpresaId da sessão: {SessionName}", sessionName);
                return;
            }

            await NotificarStatusSessaoAsync(empresaId, sessionName, status, default);
        }

        /// <summary>
        /// Notifica que um QR Code foi gerado (compatibilidade)
        /// </summary>
        public async Task NotificarQRCodeGeradoAsync(string sessionName, string qrCode)
        {
            try
            {
                var empresaId = ExtrairEmpresaIdDaSessao(sessionName);
                if (empresaId == null)
                {
                    _logger.LogWarning("Não foi possível extrair EmpresaId da sessão: {SessionName}", sessionName);
                    return;
                }

                var nomeGrupo = WhatsAppHub.ObterNomeGrupoEmpresa(empresaId);
                var numeroConexoes = WhatsAppHub.ObterNumeroConexoesPorEmpresa(empresaId);

                if (numeroConexoes == 0)
                {
                    _logger.LogDebug("Nenhuma conexão ativa para a empresa - EmpresaId: {EmpresaId}", empresaId);
                    return;
                }

                await _hubContext.Clients
                    .Group(nomeGrupo)
                    .SendAsync("QRCodeGerado", new
                    {
                        empresaId,
                        sessionName,
                        qrCode,
                        timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation(
                    "QR Code enviado - EmpresaId: {EmpresaId}, Session: {SessionName}, Conexões: {NumeroConexoes}",
                    empresaId, sessionName, numeroConexoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar QR Code - SessionName: {SessionName}", sessionName);
            }
        }

        /// <summary>
        /// Notifica que o WhatsApp foi conectado com sucesso (compatibilidade)
        /// </summary>
        public async Task NotificarWhatsAppConectadoAsync(string sessionName, string telefone)
        {
            try
            {
                var empresaId = ExtrairEmpresaIdDaSessao(sessionName);
                if (empresaId == null)
                {
                    _logger.LogWarning("Não foi possível extrair EmpresaId da sessão: {SessionName}", sessionName);
                    return;
                }

                var nomeGrupo = WhatsAppHub.ObterNomeGrupoEmpresa(empresaId);
                var numeroConexoes = WhatsAppHub.ObterNumeroConexoesPorEmpresa(empresaId);

                if (numeroConexoes == 0)
                {
                    _logger.LogDebug("Nenhuma conexão ativa para a empresa - EmpresaId: {EmpresaId}", empresaId);
                    return;
                }

                await _hubContext.Clients
                    .Group(nomeGrupo)
                    .SendAsync("WhatsAppConectado", new
                    {
                        empresaId,
                        sessionName,
                        telefone,
                        timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation(
                    "WhatsApp conectado notificado - EmpresaId: {EmpresaId}, Session: {SessionName}, Telefone: {Telefone}, Conexões: {NumeroConexoes}",
                    empresaId, sessionName, telefone, numeroConexoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao notificar WhatsApp conectado - SessionName: {SessionName}", sessionName);
            }
        }

        /// <summary>
        /// Notifica que o WhatsApp foi desconectado (compatibilidade)
        /// </summary>
        public async Task NotificarWhatsAppDesconectadoAsync(string sessionName, string? motivo = null)
        {
            try
            {
                var empresaId = ExtrairEmpresaIdDaSessao(sessionName);
                if (empresaId == null)
                {
                    _logger.LogWarning("Não foi possível extrair EmpresaId da sessão: {SessionName}", sessionName);
                    return;
                }

                var nomeGrupo = WhatsAppHub.ObterNomeGrupoEmpresa(empresaId);
                var numeroConexoes = WhatsAppHub.ObterNumeroConexoesPorEmpresa(empresaId);

                if (numeroConexoes == 0)
                {
                    _logger.LogDebug("Nenhuma conexão ativa para a empresa - EmpresaId: {EmpresaId}", empresaId);
                    return;
                }

                await _hubContext.Clients
                    .Group(nomeGrupo)
                    .SendAsync("WhatsAppDesconectado", new
                    {
                        empresaId,
                        sessionName,
                        motivo,
                        timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation(
                    "WhatsApp desconectado notificado - EmpresaId: {EmpresaId}, Session: {SessionName}, Motivo: {Motivo}, Conexões: {NumeroConexoes}",
                    empresaId, sessionName, motivo ?? "Não especificado", numeroConexoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao notificar WhatsApp desconectado - SessionName: {SessionName}", sessionName);
            }
        }

        /// <summary>
        /// Notifica todas as conexões de uma empresa sobre atualização de um cliente
        /// </summary>
        public async Task NotificarClienteAtualizadoAsync(
            string empresaId,
            string tipoAtualizacao,
            object cliente,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(empresaId))
                {
                    _logger.LogWarning("Tentativa de notificar atualização de cliente com empresaId vazio");
                    return;
                }

                var nomeGrupo = WhatsAppHub.ObterNomeGrupoEmpresa(empresaId);
                var numeroConexoes = WhatsAppHub.ObterNumeroConexoesPorEmpresa(empresaId);

                _logger.LogDebug(
                    "Enviando notificação de atualização de cliente - EmpresaId: {EmpresaId}, TipoAtualizacao: {TipoAtualizacao}, Conexões: {NumeroConexoes}",
                    empresaId, tipoAtualizacao, numeroConexoes);

                if (numeroConexoes == 0)
                {
                    _logger.LogDebug(
                        "Nenhuma conexão ativa para a empresa - EmpresaId: {EmpresaId}",
                        empresaId);
                    return;
                }

                // Envia notificação para todos os clientes do grupo da empresa
                await _hubContext.Clients
                    .Group(nomeGrupo)
                    .SendAsync(
                        "ClienteAtualizado",
                        new
                        {
                            empresaId,
                            tipoAtualizacao,
                            cliente,
                            timestamp = DateTime.UtcNow
                        },
                        cancellationToken);

                _logger.LogInformation(
                    "Notificação de atualização de cliente enviada - EmpresaId: {EmpresaId}, TipoAtualizacao: {TipoAtualizacao}, Conexões notificadas: {NumeroConexoes}",
                    empresaId, tipoAtualizacao, numeroConexoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao enviar notificação de atualização de cliente - EmpresaId: {EmpresaId}, TipoAtualizacao: {TipoAtualizacao}",
                    empresaId, tipoAtualizacao);
            }
        }
    }
}
