namespace Client_Service.Service.Interface
{
    /// <summary>
    /// Serviço para autenticação automática via webhook baseado em número do WhatsApp ou Session Name
    /// </summary>
    public interface IWebhookAuthService
    {
        /// <summary>
        /// Identifica a empresa pelo nome da sessão WAHA e configura o contexto do tenant
        /// </summary>
        /// <param name="sessionName">Nome da sessão WAHA (ex: "TesteVitinhoMuitoLoko")</param>
        /// <returns>EmpresaId e UsuarioId configurados, ou null se não encontrado</returns>
        Task<WebhookAuthResult?> AutenticarPorSessionAsync(string sessionName);

        /// <summary>
        /// Identifica a empresa pelo número do WhatsApp que recebeu a mensagem
        /// e configura o contexto do tenant
        /// </summary>
        /// <param name="numeroWhatsApp">Número do WhatsApp que recebeu a mensagem (ex: 554184724179)</param>
        /// <returns>EmpresaId e UsuarioId configurados, ou null se não encontrado</returns>
        Task<WebhookAuthResult?> AutenticarPorNumeroWhatsAppAsync(string numeroWhatsApp);

        /// <summary>
        /// Identifica a empresa pelo ID e configura o contexto do tenant
        /// </summary>
        /// <param name="empresaId">ID da empresa (ex: 690e3a0cbd0162a5bd7d724d)</param>
        /// <returns>EmpresaId e UsuarioId configurados, ou null se não encontrado</returns>
        Task<WebhookAuthResult?> AutenticarPorEmpresaIdAsync(string empresaId);

        /// <summary>
        /// Extrai o número limpo do formato WAHA (remove @c.us, @s.whatsapp.net, etc)
        /// </summary>
        /// <param name="numeroWaha">Número no formato WAHA</param>
        /// <returns>Número limpo</returns>
        string ExtrairNumeroLimpo(string numeroWaha);
    }

    public class WebhookAuthResult
    {
        public string EmpresaId { get; set; }
        public string UsuarioId { get; set; }
        public string ConnectionString { get; set; }
        public string NomeBaseDados { get; set; }
        public string? NumeroWhatsApp { get; set; }
        public string? SessionName { get; set; }
        public bool Sucesso { get; set; }
        public string? MensagemErro { get; set; }
    }
}
