using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Shared.Hubs
{
    /// <summary>
    /// Hub SignalR para notificações em tempo real de WhatsApp por empresa
    /// Cada conexão é automaticamente adicionada ao grupo da sua empresa
    /// </summary>
    [Authorize]
    public class WhatsAppHub : Hub
    {
        // Rastreamento de empresas e conexões
        private static readonly ConcurrentDictionary<string, HashSet<string>> _empresaConexoes = new();
        private static readonly ConcurrentDictionary<string, string> _conexaoEmpresa = new();

        private readonly ILogger<WhatsAppHub> _logger;

        public WhatsAppHub(ILogger<WhatsAppHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Chamado quando um cliente se conecta ao Hub
        /// Automaticamente adiciona a conexão ao grupo da empresa
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

                // Adiciona automaticamente ao grupo da empresa
                await Groups.AddToGroupAsync(connectionId, $"empresa_{empresaId}");

                // Rastreia a conexão
                _empresaConexoes.AddOrUpdate(
                    empresaId,
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

                _conexaoEmpresa[connectionId] = empresaId;

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

                // Remove a conexão do grupo da empresa
                if (_conexaoEmpresa.TryRemove(connectionId, out var empresaId))
                {
                    if (_empresaConexoes.TryGetValue(empresaId, out var conexoes))
                    {
                        lock (conexoes)
                        {
                            conexoes.Remove(connectionId);

                            // Remove o grupo se não houver mais conexões
                            if (conexoes.Count == 0)
                            {
                                _empresaConexoes.TryRemove(empresaId, out _);
                            }
                        }
                    }

                    await Groups.RemoveFromGroupAsync(connectionId, $"empresa_{empresaId}");
                }

                if (exception != null)
                {
                    _logger.LogWarning(exception,
                        "Cliente desconectado com erro do WhatsApp Hub - ConnectionId: {ConnectionId}, EmpresaId: {EmpresaId}",
                        connectionId, empresaId);
                }
                else
                {
                    _logger.LogInformation(
                        "Cliente desconectado do WhatsApp Hub - ConnectionId: {ConnectionId}, EmpresaId: {EmpresaId}",
                        connectionId, empresaId);
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
        /// Obtém o número de conexões ativas para uma empresa
        /// </summary>
        public static int ObterNumeroConexoesPorEmpresa(string empresaId)
        {
            if (_empresaConexoes.TryGetValue(empresaId, out var conexoes))
            {
                lock (conexoes)
                {
                    return conexoes.Count;
                }
            }
            return 0;
        }

        /// <summary>
        /// Verifica se há conexões ativas para uma empresa
        /// </summary>
        public static bool TemConexoesAtivas(string empresaId)
        {
            return _empresaConexoes.ContainsKey(empresaId) && ObterNumeroConexoesPorEmpresa(empresaId) > 0;
        }

        /// <summary>
        /// Obtém o nome do grupo SignalR para uma empresa
        /// </summary>
        public static string ObterNomeGrupoEmpresa(string empresaId)
        {
            return $"empresa_{empresaId}";
        }
    }
}
