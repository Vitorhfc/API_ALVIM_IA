using Client_Service.ServiceGenerico.Interface;
using Shared.Classes.Entidades.Client;

namespace Client_Service.Service.Interface
{
    public interface IProcessamentoIAService
    {
        Task<IEnumerable<ProcessamentoIA>> BuscarPorMensagemAsync(string mensagemId);
        Task<IEnumerable<ProcessamentoIA>> BuscarPorClienteAsync(string clienteId);
        Task<ProcessamentoIA?> BuscarPorGrupoAsync(string grupoId);

        Task<IEnumerable<ProcessamentoIA>> BuscarPorStatusAsync(StatusProcessamento status);
        Task<IEnumerable<ProcessamentoIA>> BuscarPorStatusAsync(string status);

        Task<ProcessamentoIA?> BuscarUltimoProcessamentoAsync(string clienteId);
        Task<IEnumerable<ProcessamentoIA>> BuscarProcessamentosFalhadosAsync();
        Task<IEnumerable<ProcessamentoIA>> BuscarProcessamentosSucessoAsync(string clienteId);

        Task<IEnumerable<ProcessamentoIA>> BuscarPorPeriodoAsync(string clienteId, DateTime dataInicio, DateTime dataFim);

        /// <summary>
        /// Processa UMA mensagem individual enviando diretamente para N8N (NOVO FLUXO - Assíncrono)
        /// Não aguarda resposta do N8N, apenas envia a mensagem
        /// O N8N fará agrupamento e processamento, depois enviará callback com a resposta
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <param name="mensagemId">ID da mensagem a processar</param>
        /// <param name="carregarHistorico">Se true, o N8N carregará histórico de mensagens. Se false, processa apenas esta mensagem</param>
        Task ProcessarMensagemIndividualAsync(string clienteId, string mensagemId, bool carregarHistorico = false);

        /// <summary>
        /// Processa uma mensagem individual pelo ID, determinando automaticamente o cliente
        /// </summary>
        /// <param name="mensagemId">ID da mensagem a processar</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task ProcessarMensagemAsync(string mensagemId, CancellationToken cancellationToken = default);
    }
}