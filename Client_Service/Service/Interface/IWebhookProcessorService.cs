using Shared.Classes.Model;
using System.Threading;
using System.Threading.Tasks;

namespace Client_Service.Service.Interface
{
    /// <summary>
    /// Interface para processamento de webhooks do WAHA
    /// </summary>
    public interface IWebhookProcessorService
    {
        /// <summary>
        /// Valida o webhook antes do processamento
        /// </summary>
        /// <param name="payloadString">String do payload JSON</param>
        /// <param name="empresaId">ID da empresa</param>
        /// <returns>Resultado da validação</returns>
        Task<ValidacaoWebhookResult> ValidarWebhook(string payloadString, string empresaId);

        /// <summary>
        /// Processa webhook recebido do WAHA
        /// </summary>
        /// <param name="webhook">Dados do webhook</param>
        /// <param name="empresaId">ID da empresa</param>
        /// <param name="idLog">ID único para rastreamento</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado do processamento</returns>
        Task<ProcessamentoWebhookResult> ProcessarWebhook(
            WebhookWaHaRequest webhook,
            string empresaId,
            string idLog,
            CancellationToken cancellationToken);
    }
}