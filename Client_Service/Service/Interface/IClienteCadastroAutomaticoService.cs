using Shared.Classes.Entidades.Client;
using Shared.Messaging.Models;

namespace Client_Service.Service.Interface
{
    /// <summary>
    /// Serviço para cadastro automático de clientes via webhook
    /// </summary>
    public interface IClienteCadastroAutomaticoService
    {
        /// <summary>
        /// Busca ou cria um cliente baseado nas informações do webhook
        /// </summary>
        /// <param name="webhookEvent">Evento do webhook normalizado</param>
        /// <returns>Cliente existente ou recém-criado</returns>
        Task<Cliente> BuscarOuCriarClienteAsync(StandardWhatsAppEvent webhookEvent);

        /// <summary>
        /// Atualiza informações do cliente se necessário
        /// </summary>
        /// <param name="cliente">Cliente a ser atualizado</param>
        /// <param name="webhookEvent">Dados do webhook</param>
        /// <returns>True se houve atualização</returns>
        Task<bool> AtualizarInformacoesClienteAsync(Cliente cliente, StandardWhatsAppEvent webhookEvent);
    }
}
