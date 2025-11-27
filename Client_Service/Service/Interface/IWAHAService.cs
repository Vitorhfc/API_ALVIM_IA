using Shared.Classes.Model;

namespace Client_Service.Service.Interface
{
    /// <summary>
    /// Interface para serviços relacionados à API WAHA (WhatsApp HTTP API)
    /// </summary>
    public interface IWAHAService
    {
        /// <summary>
        /// Inicia uma nova sessão WAHA e retorna o QR Code para conexão
        /// </summary>
        Task<WAHAQRCodeResponse> IniciarSessaoAsync(string sessionName);

        /// <summary>
        /// Obtém o QR Code de uma sessão existente
        /// </summary>
        Task<WAHAQRCodeResponse> ObterQRCodeAsync(string sessionName);

        /// <summary>
        /// Verifica o status de uma sessão WAHA
        /// </summary>
        Task<WAHAStatusResponse> ObterStatusSessaoAsync(string sessionName);

        /// <summary>
        /// Para/desconecta uma sessão WAHA
        /// </summary>
        Task<WAHAActionResponse> PararSessaoAsync(string sessionName);

        /// <summary>
        /// Remove uma sessão WAHA completamente
        /// </summary>
        Task<WAHAActionResponse> RemoverSessaoAsync(string sessionName);

        /// <summary>
        /// Lista todas as sessões WAHA disponíveis
        /// </summary>
        Task<List<WAHASessionInfo>> ListarSessoesAsync();

        /// <summary>
        /// Reinicia uma sessão WAHA
        /// </summary>
        Task<WAHAActionResponse> ReiniciarSessaoAsync(string sessionName);

        /// <summary>
        /// Obtém informações sobre a conta conectada
        /// </summary>
        Task<WAHAAccountInfo> ObterInformacoesContaAsync(string sessionName);

        /// <summary>
        /// Gera o nome da sessão no padrão: Alvim_{empresaId}_{ddMMyyyy}
        /// </summary>
        string GerarNomeSessao(string empresaId);

        /// <summary>
        /// Gera o nome da sessão e salva na empresa no banco de dados
        /// </summary>
        Task<string> GerarESalvarNomeSessaoAsync(string empresaId);
    }
}
