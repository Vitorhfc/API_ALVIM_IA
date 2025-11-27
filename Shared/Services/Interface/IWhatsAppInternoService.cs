using Shared.Classes.Model;

namespace Shared.Services.Interface
{
    /// <summary>
    /// Interface para serviço de WhatsApp interno com credenciais predefinidas
    /// Usado para enviar mensagens internas do sistema (2FA, notificações, etc.)
    /// </summary>
    public interface IWhatsAppInternoService
    {
        /// <summary>
        /// Envia mensagem de texto usando credenciais internas predefinidas
        /// </summary>
        Task<EnvioResponse> EnviarTextoAsync(string numeroDestino, string mensagem);

        /// <summary>
        /// Envia mensagem com mídia usando credenciais internas predefinidas
        /// </summary>
        Task<EnvioResponse> EnviarMidiaAsync(
            string numeroDestino,
            string mensagem,
            string urlMidia,
            TipoMensagemWhatsApp tipo);
    }
}
