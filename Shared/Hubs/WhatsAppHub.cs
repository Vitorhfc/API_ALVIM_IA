using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Shared.Hubs
{
    /// <summary>
    /// Hub SignalR para notificações em tempo real de WhatsApp (WAHA)
    /// </summary>
    [Authorize]
    public class WhatsAppHub : Hub
    {
        // Rastreamento de sessões e conexões
        private static readonly ConcurrentDictionary<string, HashSet<string>> _sessaoConexoes = new();
        private static readonly ConcurrentDictionary<string, string> _conexaoSessao = new();

        private readonly ILogger<WhatsAppHub> _logger;

        public WhatsAppHub(ILogger<WhatsAppHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Conecta o cliente a um grupo específico de sessão WhatsApp
        /// Isso permite enviar notificações apenas para clientes interessados em uma sessão específica
        /// </summary>
        /// <param name="sessionName">Nome da sessão WhatsApp (ex: Alvim_123_10112025)</param>
        public async Task JoinGroup(string sessionName)
        {
            if (string.IsNullOrWhiteSpace(sessionName))
            {
                _logger.LogWarning("Tentativa de JoinGroup com sessionName vazio - ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
                return;
            }

            var connectionId = Context.ConnectionId;
            var empresaId = ObterEmpresaId();

            await Groups.AddToGroupAsync(connectionId, sessionName);

            // Rastreia a conexão
            _sessaoConexoes.AddOrUpdate(
                sessionName,
                new HashSet<string> { connectionId },
                (key, existingSet) =>
                {
                    lock (existingSet)
                    {
                        existingSet.Add(connectionId);
                    }
                    return existingSet;
                }
            );

            _conexaoSessao[connectionId] = sessionName;

            _logger.LogInformation(
                "Cliente inscrito no grupo WhatsApp - ConnectionId: {ConnectionId}, SessionName: {SessionName}, EmpresaId: {EmpresaId}",
                connectionId, sessionName, empresaId
            );
        }

        /// <summary>
        /// Remove o cliente de um grupo de sessão
        /// </summary>
        /// <param name="sessionName">Nome da sessão WhatsApp</param>
        public async Task LeaveGroup(string sessionName)
        {
            if (string.IsNullOrWhiteSpace(sessionName))
            {
                _logger.LogWarning("Tentativa de LeaveGroup com sessionName vazio - ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
                return;
            }

            var connectionId = Context.ConnectionId;

            await Groups.RemoveFromGroupAsync(connectionId, sessionName);

            // Remove do rastreamento
            if (_sessaoConexoes.TryGetValue(sessionName, out var conexoes))
            {
                lock (conexoes)
                {
                    conexoes.Remove(connectionId);
                    if (conexoes.Count == 0)
                    {
                        _sessaoConexoes.TryRemove(sessionName, out _);
                    }
                }
            }

            _conexaoSessao.TryRemove(connectionId, out _);

            _logger.LogInformation(
                "Cliente removido do grupo WhatsApp - ConnectionId: {ConnectionId}, SessionName: {SessionName}",
                connectionId, sessionName
            );
        }

        /// <summary>
        /// Chamado quando um cliente se conecta ao Hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                var empresaId = ObterEmpresaId();
                var usuarioId = ObterUsuarioId();
                var connectionId = Context.ConnectionId;

                if (string.IsNullOrEmpty(empresaId))
                {
                    _logger.LogWarning("Tentativa de conexão sem EmpresaId - ConnectionId: {ConnectionId}", connectionId);
                    Context.Abort();
                    return;
                }

                _logger.LogInformation(
                    "Cliente conectado ao WhatsApp Hub - ConnectionId: {ConnectionId}, EmpresaId: {EmpresaId}, UsuarioId: {UsuarioId}",
                    connectionId, empresaId, usuarioId
                );

                // Notifica o cliente que está conectado
                await Clients.Caller.SendAsync("OnConnected", new
                {
                    connectionId,
                    empresaId,
                    usuarioId,
                    timestamp = DateTime.UtcNow,
                    message = "Conectado ao sistema de notificações WhatsApp"
                });

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar cliente ao WhatsApp Hub - ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
                throw;
            }
        }

        /// <summary>
        /// Chamado quando um cliente se desconecta do Hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var connectionId = Context.ConnectionId;

                // Remove a conexão de todos os grupos
                if (_conexaoSessao.TryRemove(connectionId, out var sessionName))
                {
                    if (_sessaoConexoes.TryGetValue(sessionName, out var conexoes))
                    {
                        lock (conexoes)
                        {
                            conexoes.Remove(connectionId);

                            // Remove o grupo se não houver mais conexões
                            if (conexoes.Count == 0)
                            {
                                _sessaoConexoes.TryRemove(sessionName, out _);
                            }
                        }
                    }

                    await Groups.RemoveFromGroupAsync(connectionId, sessionName);
                }

                if (exception != null)
                {
                    _logger.LogWarning(exception,
                        "Cliente desconectado com erro do WhatsApp Hub - ConnectionId: {ConnectionId}, SessionName: {SessionName}",
                        connectionId, sessionName);
                }
                else
                {
                    _logger.LogInformation(
                        "Cliente desconectado do WhatsApp Hub - ConnectionId: {ConnectionId}, SessionName: {SessionName}",
                        connectionId, sessionName);
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desconectar cliente do WhatsApp Hub - ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
            }
        }

        /// <summary>
        /// Método que permite ao cliente enviar uma mensagem de ping
        /// </summary>
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", new
            {
                timestamp = DateTime.UtcNow,
                connectionId = Context.ConnectionId
            });
        }

        /// <summary>
        /// Obtém o ID da empresa do contexto atual
        /// </summary>
        private string? ObterEmpresaId()
        {
            return Context.User?.FindFirst("EmpresaId")?.Value;
        }

        /// <summary>
        /// Obtém o ID do usuário do contexto atual
        /// </summary>
        private string? ObterUsuarioId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                   Context.User?.FindFirst("UsuarioId")?.Value;
        }

        /// <summary>
        /// Obtém o número de conexões ativas para uma sessão
        /// </summary>
        public static int ObterNumeroConexoesPorSessao(string sessionName)
        {
            if (_sessaoConexoes.TryGetValue(sessionName, out var conexoes))
            {
                lock (conexoes)
                {
                    return conexoes.Count;
                }
            }
            return 0;
        }

        /// <summary>
        /// Verifica se há conexões ativas para uma sessão
        /// </summary>
        public static bool TemConexoesAtivas(string sessionName)
        {
            return _sessaoConexoes.ContainsKey(sessionName) && ObterNumeroConexoesPorSessao(sessionName) > 0;
        }
    }
}
