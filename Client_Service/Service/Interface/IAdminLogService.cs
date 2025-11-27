using Microsoft.AspNetCore.Http;

namespace Client_Service.Service.Interface
{
    /// <summary>
    /// Serviço para registrar logs no ambiente Admin quando o contexto Client não está disponível
    /// </summary>
    public interface IAdminLogService
    {
        /// <summary>
        /// Registra um log de webhook WAHA no ambiente Admin
        /// </summary>
        /// <param name="idLog">ID único do log</param>
        /// <param name="empresaId">ID da empresa (opcional)</param>
        /// <param name="tipoEvento">Tipo do evento (ex: "ENTRADA", "SAIDA_SUCESSO")</param>
        /// <param name="acao">Ação realizada</param>
        /// <param name="payload">Payload original</param>
        /// <param name="payloadLimpo">Payload limpo (opcional)</param>
        /// <param name="sucesso">Indica se foi sucesso</param>
        /// <param name="mensagemErro">Mensagem de erro (opcional)</param>
        /// <param name="stackTrace">Stack trace do erro (opcional)</param>
        /// <param name="httpContext">Contexto HTTP para extrair informações (opcional)</param>
        Task RegistrarWebhookWahaAsync(
            string idLog,
            string? empresaId,
            string tipoEvento,
            string acao,
            string payload,
            string? payloadLimpo = null,
            bool sucesso = true,
            string? mensagemErro = null,
            string? stackTrace = null,
            HttpContext? httpContext = null);

        /// <summary>
        /// Registra um log genérico no ambiente Admin
        /// </summary>
        /// <param name="origem">Origem do log</param>
        /// <param name="acao">Ação realizada</param>
        /// <param name="descricao">Descrição do log</param>
        /// <param name="sucesso">Indica se foi sucesso</param>
        /// <param name="empresaId">ID da empresa (opcional)</param>
        /// <param name="metadados">Metadados adicionais (opcional)</param>
        Task RegistrarLogGenericoAsync(
            string origem,
            string acao,
            string descricao,
            bool sucesso = true,
            string? empresaId = null,
            Dictionary<string, string>? metadados = null);
    }
}
