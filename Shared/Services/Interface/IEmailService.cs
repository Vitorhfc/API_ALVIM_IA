using Shared.Classes.Model;

namespace Shared.Services.Interface
{
    /// <summary>
    /// Serviço genérico para envio de emails
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Envia email usando a API Resend
        /// </summary>
        Task<EnvioResponse> EnviarEmailAsync(EnviarEmailRequest request);

        /// <summary>
        /// Envia email simples (texto)
        /// </summary>
        Task<EnvioResponse> EnviarEmailSimplesAsync(string destinatario, string assunto, string corpo);

        /// <summary>
        /// Envia email HTML
        /// </summary>
        Task<EnvioResponse> EnviarEmailHTMLAsync(string destinatario, string assunto, string corpoHTML);

        /// <summary>
        /// Envia email para múltiplos destinatários
        /// </summary>
        Task<EnvioResponse> EnviarEmailMultiplosDestinatariosAsync(
            List<string> destinatarios,
            string assunto,
            string corpo,
            bool html = true);
    }
}
