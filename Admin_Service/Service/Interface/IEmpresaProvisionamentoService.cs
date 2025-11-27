using Shared.Classes.Entidades.ADM;
using Shared.Classes.Model;

namespace Admin_Service.Service.Interface
{
    /// <summary>
    /// Serviço para provisionamento automático de empresas (instância WAHA, database, etc)
    /// </summary>
    public interface IEmpresaProvisionamentoService
    {
        /// <summary>
        /// Provisiona completamente uma empresa (cria instância WAHA, configura webhook, etc)
        /// </summary>
        Task<ResultadoProvisionamento> ProvisionarEmpresaAsync(Empresa empresa);

        /// <summary>
        /// Reconecta uma instância WAHA existente
        /// </summary>
        Task<bool> ReconectarInstanciaAsync(string empresaId);

        /// <summary>
        /// Verifica e atualiza o status de conexão da instância
        /// </summary>
        Task<StatusConexao> VerificarStatusConexaoAsync(string empresaId);

        /// <summary>
        /// Configura ou reconfigura WAHA para uma empresa
        /// </summary>
        Task<ConfigurarWahaResponse> ConfigurarOuReconfigurarWahaAsync(
            string empresaId,
            ConfigurarWahaRequest request);
    }

    /// <summary>
    /// Resultado do provisionamento de empresa
    /// </summary>
    public class ResultadoProvisionamento
    {
        public bool Sucesso { get; set; }
        public string? Mensagem { get; set; }
        public bool InstanciaWahaCriada { get; set; }
        public bool WebhookConfigurado { get; set; }
        public string? Erro { get; set; }
        public List<string> Logs { get; set; } = new();
    }

    /// <summary>
    /// Status de conexão da instância WAHA
    /// </summary>
    public class StatusConexao
    {
        public bool Conectado { get; set; }
        public string? Status { get; set; }
        public string? Mensagem { get; set; }
        public DateTime? UltimaVerificacao { get; set; }
    }
}
