using Shared.Classes.Model;

namespace Client_Service.Service.Interface
{
    public interface IN8NService
    {
        /// <summary>
        /// Obtém os planos/features habilitados para um cliente específico
        /// </summary>
        Task<PlanoClienteResponse> ObterPlanoClienteAsync(string idCliente);

        /// <summary>
        /// Obtém o contexto completo do projeto configurado para o cliente
        /// </summary>
        Task<ContextoProjetoResponse> ObterContextoProjetoAsync(string idCliente);

        /// <summary>
        /// Obtém a lista de documentos configurados pelo cliente
        /// </summary>
        Task<DocumentosResponse> ObterDocumentosAsync(string idCliente);

        /// <summary>
        /// Envia UMA mensagem para o N8N sem esperar resposta (fire and forget)
        /// O N8N fará o agrupamento e processamento internamente
        /// </summary>
        Task EnviarMensagemParaN8NAsync(N8NEnviarMensagemUnicaRequest request);

        /// <summary>
        /// Processa callback recebido do N8N com a resposta da IA
        /// Este método recebe as mensagens processadas, salva no banco e envia para o WhatsApp
        /// </summary>
        Task ProcessarCallbackRespostaAsync(N8NCallbackRespostaRequest request);

        /// <summary>
        /// Obtém as mensagens dos últimos 2 dias de um cliente específico
        /// </summary>
        /// <param name="idCliente">ID do cliente</param>
        /// <returns>Lista de mensagens dos últimos 2 dias</returns>
        Task<List<MensagemN8N>> ObterMensagensRecentesAsync(string idCliente);
    }
}
