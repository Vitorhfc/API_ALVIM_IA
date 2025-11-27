using Microsoft.AspNetCore.Http;
using Shared.Classes.Results;

namespace Client_Service.Service.Interface
{
    /// <summary>
    /// Serviço orquestrador de webhook - coordena todo o fluxo de processamento
    /// </summary>
    public interface IWebhookOrchestratorService
    {
        /// <summary>
        /// Processa o webhook de forma orquestrada
        /// Inclui: validação, autenticação, log e processamento
        /// </summary>
        /// <param name="empresaId">ID da empresa</param>
        /// <param name="payloadString">Payload JSON do webhook</param>
        /// <param name="httpContext">Contexto HTTP da requisição</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da orquestração</returns>
        Task<WebhookOrchestrationResult> ProcessarWebhookAsync(
            string empresaId,
            string payloadString,
            HttpContext httpContext,
            CancellationToken cancellationToken);
    }
}
