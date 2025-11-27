using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Cliente.Controllers
{
    /// <summary>
    /// Controller simplificado para recepção de webhooks WAHA
    /// Delega toda a lógica de processamento para o WebhookOrchestratorService
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookWahaController : ControllerBase
    {
        private readonly IWebhookOrchestratorService _orchestrator;
        private readonly ILogger<WebhookWahaController> _logger;

        public WebhookWahaController(
            IWebhookOrchestratorService orchestrator,
            ILogger<WebhookWahaController> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        /// <summary>
        /// Recebe webhook do WAHA e delega processamento para o orquestrador
        /// </summary>
        [AllowAnonymous]
        [RequestSizeLimit(50_000_000)]
        [HttpPost("waha/{empresaId}")]
        public async Task<IActionResult> ReceberWebhookWaha(
            [FromRoute] string empresaId,
            [FromBody] JsonElement payload,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Webhook recebido - EmpresaId: {EmpresaId}", empresaId);

                var payloadString = payload.GetRawText();

                // Delegar todo o processamento para o orquestrador
                var resultado = await _orchestrator.ProcessarWebhookAsync(
                    empresaId,
                    payloadString,
                    HttpContext,
                    cancellationToken
                );

                // Retornar resposta baseada no resultado da orquestração
                return Ok(resultado.ResponseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico não tratado no controller - EmpresaId: {EmpresaId}", empresaId);

                return Ok(new
                {
                    mensagem = "Erro crítico no processamento",
                    erro = "Erro interno"
                });
            }
        }
    }
}
