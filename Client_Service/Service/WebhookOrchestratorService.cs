using Admin_Service.Service.Interface;
using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Classes.Results;

namespace Client_Service.Service
{
    /// <summary>
    /// Orquestrador centralizado de webhook
    /// Responsável por coordenar todo o fluxo: validação → autenticação → log → processamento
    /// </summary>
    public class WebhookOrchestratorService : IWebhookOrchestratorService
    {
        private readonly IWebhookProcessorService _webhookProcessor;
        private readonly IWebhookAuthCacheService _authCache;
        private readonly ILogClientService _logClient;
        private readonly IAdminLogService _adminLog;
        private readonly ILogger<WebhookOrchestratorService> _logger;

        public WebhookOrchestratorService(
            IWebhookProcessorService webhookProcessor,
            IWebhookAuthCacheService authCache,
            ILogClientService logClient,
            IAdminLogService adminLog,
            ILogger<WebhookOrchestratorService> logger)
        {
            _webhookProcessor = webhookProcessor;
            _authCache = authCache;
            _logClient = logClient;
            _adminLog = adminLog;
            _logger = logger;
        }

        public async Task<WebhookOrchestrationResult> ProcessarWebhookAsync(
            string empresaId,
            string payloadString,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            var idLog = Guid.NewGuid().ToString();

            try
            {
                // === ETAPA 1: LOG DE ENTRADA ===
                await RegistrarLogEntradaAsync(idLog, payloadString, empresaId, httpContext);

                // === ETAPA 2: VALIDAÇÃO ===
                var validacao = await _webhookProcessor.ValidarWebhook(payloadString, empresaId);

                if (!validacao.EhValido)
                {
                    await RegistrarRespostaAsync(idLog, validacao.TipoErro!, new { mensagem = validacao.MensagemErro }, empresaId, httpContext);
                    return WebhookOrchestrationResult.Fail(validacao.TipoErro, validacao.MensagemErro.ToString());
                }

                if (validacao.DeveIgnorar)
                {
                    await RegistrarRespostaAsync(idLog, "SAIDA_IGNORADO", new { mensagem = validacao.MensagemErro }, empresaId, httpContext);
                    return WebhookOrchestrationResult.Ok("SAIDA_IGNORADO", new { mensagem = validacao.MensagemErro });
                }

                // === ETAPA 3: AUTENTICAÇÃO COM CACHE ===
                var authResult = await _authCache.GetOrCreateAuthDataAsync(empresaId, cancellationToken);

                if (authResult == null || !authResult.Sucesso)
                {
                    var erro = new { mensagem = "Empresa não identificada", empresaId, erro = authResult?.MensagemErro };
                    await RegistrarRespostaAsync(idLog, "SAIDA_FALHA_AUTENTICACAO", erro, empresaId, httpContext);
                    return WebhookOrchestrationResult.Fail("SAIDA_FALHA_AUTENTICACAO", authResult?.MensagemErro ?? "Falha na autenticação");
                }

                // === ETAPA 4: CONFIGURAR CONTEXTO ===
                ConfigurarContextoHttp(httpContext, authResult.EmpresaId, authResult.UsuarioId);

                // === ETAPA 5: LOG DO WEBHOOK (CLIENT) ===
                try
                {
                    await _logClient.RegistrarWebhook(
                        idLog,
                        "WAHA",
                        validacao.WebhookRequest!.@event ?? "unknown",
                        JsonConvert.SerializeObject(validacao.WebhookRequest)
                    );
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Falha ao registrar webhook no Client - IdLog: {IdLog}", idLog);
                }

                // === ETAPA 6: PROCESSAMENTO ===
                var resultado = await _webhookProcessor.ProcessarWebhook(
                    validacao.WebhookRequest!,
                    empresaId,
                    idLog,
                    cancellationToken
                );

                // === ETAPA 7: RESPOSTA ===
                if (resultado.Sucesso)
                {
                    var sucesso = new { mensagem = "Webhook processado com sucesso", idProcessamento = resultado.IdProcessamento };
                    await RegistrarRespostaAsync(idLog, "SAIDA_SUCESSO", sucesso, empresaId, httpContext);
                    return WebhookOrchestrationResult.Ok("SAIDA_SUCESSO", sucesso);
                }
                else
                {
                    var falha = new { mensagem = "Erro no processamento", erro = resultado.Erro };
                    await RegistrarRespostaAsync(idLog, "SAIDA_ERRO_PROCESSAMENTO", falha, empresaId, httpContext);
                    return WebhookOrchestrationResult.Fail("SAIDA_ERRO_PROCESSAMENTO", resultado.Erro ?? "Erro desconhecido");
                }
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Erro JSON ao processar webhook - IdLog: {IdLog}", idLog);
                await RegistrarErroAsync(idLog, "ERRO_JSON", jsonEx, payloadString, empresaId, httpContext);
                return WebhookOrchestrationResult.Fail("SAIDA_ERRO_JSON", "Formato JSON inválido");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar webhook - IdLog: {IdLog}", idLog);
                await RegistrarErroAsync(idLog, "ERRO_INTERNO", ex, payloadString, empresaId, httpContext);
                return WebhookOrchestrationResult.Fail("SAIDA_ERRO_INTERNO", "Erro interno no processamento");
            }
        }

        #region Métodos Privados de Log

        private async Task RegistrarLogEntradaAsync(
            string idLog,
            string payload,
            string? empresaId,
            HttpContext httpContext)
        {
            try
            {
                // Registra sempre no Admin
                await _adminLog.RegistrarWebhookWahaAsync(
                    idLog: idLog,
                    empresaId: empresaId,
                    tipoEvento: "ENTRADA",
                    acao: "WebhookOrchestrator.Entrada",
                    payload: payload,
                    sucesso: true,
                    httpContext: httpContext
                );

                // Tenta registrar no Client (pode falhar se não autenticado ainda)
                try
                {
                    await _logClient.RegistrarWebhook(idLog, "WebhookWaha", "ENTRADA", payload);
                }
                catch
                {
                    // Ignora erro (esperado antes da autenticação)
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao registrar log de entrada - IdLog: {IdLog}", idLog);
            }
        }

        private async Task RegistrarRespostaAsync(
            string idLog,
            string tipoSaida,
            object mensagem,
            string? empresaId,
            HttpContext httpContext)
        {
            try
            {
                var mensagemJson = JsonConvert.SerializeObject(mensagem);
                var sucesso = !tipoSaida.Contains("ERRO") && !tipoSaida.Contains("FALHA");

                // Registra no Admin
                await _adminLog.RegistrarWebhookWahaAsync(
                    idLog: idLog,
                    empresaId: empresaId,
                    tipoEvento: tipoSaida,
                    acao: "WebhookOrchestrator.Resposta",
                    payload: mensagemJson,
                    sucesso: sucesso,
                    httpContext: httpContext
                );

                // Tenta registrar no Client
                try
                {
                    await _logClient.RegistrarWebhook(idLog, "WebhookWaha", tipoSaida, mensagemJson);
                }
                catch
                {
                    // Ignora erro
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao registrar resposta - IdLog: {IdLog}", idLog);
            }
        }

        private async Task RegistrarErroAsync(
            string idLog,
            string tipoErro,
            Exception exception,
            string payload,
            string? empresaId,
            HttpContext httpContext)
        {
            try
            {
                // Registra no Admin
                await _adminLog.RegistrarWebhookWahaAsync(
                    idLog: idLog,
                    empresaId: empresaId,
                    tipoEvento: tipoErro,
                    acao: "WebhookOrchestrator.Erro",
                    payload: payload,
                    sucesso: false,
                    mensagemErro: exception.Message,
                    stackTrace: exception.StackTrace,
                    httpContext: httpContext
                );

                // Tenta registrar no Client
                try
                {
                    await _logClient.LogErroAsync(exception, "ProcessarWebhookAsync", "WebhookOrchestrator", $"IdLog: {idLog}");
                }
                catch
                {
                    // Ignora erro
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao registrar erro - IdLog: {IdLog}", idLog);
            }
        }

        private void ConfigurarContextoHttp(HttpContext httpContext, string empresaId, string usuarioId)
        {
            httpContext.Items["EmpresaId"] = empresaId;
            httpContext.Items["UsuarioId"] = usuarioId;
        }

        #endregion
    }
}
