using Microsoft.Extensions.Logging;
using Shared.Classes.Model;
using Shared.Services.Interface;

namespace Shared.Services
{
    /// <summary>
    /// Serviço de WhatsApp interno com credenciais predefinidas
    /// Usado para enviar mensagens internas do sistema (2FA, notificações, etc.)
    /// </summary>
    public class WhatsAppInternoService : IWhatsAppInternoService
    {
        private readonly IWhatsAppService _whatsAppService;
        private readonly ILogger<WhatsAppInternoService> _logger;

        // Credenciais predefinidas para o sistema interno
        private const string ENDPOINT = "http://49.12.200.232:8084/";
        private const string API_KEY = "z1Yz2P73oc4s-t55x44h637S8-s3c4e5fe6w46-FZTCu4ehnM8v4hu";
        private const string INSTANCE_DEFAULT = "Opita-3f6e9a7e-2c4b-4b89-89c8-1e80e7fcfdf2";

        public WhatsAppInternoService(
            IWhatsAppService whatsAppService,
            ILogger<WhatsAppInternoService> logger)
        {
            _whatsAppService = whatsAppService;
            _logger = logger;
        }

        /// <summary>
        /// Envia mensagem de texto usando credenciais internas predefinidas
        /// </summary>
        public async Task<EnvioResponse> EnviarTextoAsync(string numeroDestino, string mensagem)
        {
            try
            {
                _logger.LogInformation(
                    "Enviando mensagem WhatsApp interna - Para: {Numero}",
                    numeroDestino
                );

                var resultado = await _whatsAppService.EnviarTextoAsync(
                    numeroDestino: numeroDestino,
                    mensagem: mensagem,
                    session: INSTANCE_DEFAULT,
                    wahaApiUrl: ENDPOINT,
                    wahaApiKey: API_KEY
                );

                if (resultado.Sucesso)
                {
                    _logger.LogInformation(
                        "Mensagem WhatsApp interna enviada com sucesso - Para: {Numero}",
                        numeroDestino
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Falha ao enviar mensagem WhatsApp interna - Para: {Numero}, Erro: {Erro}",
                        numeroDestino,
                        resultado.Erro
                    );
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem WhatsApp interna - Para: {Numero}", numeroDestino);

                return new EnvioResponse
                {
                    Sucesso = false,
                    Mensagem = "Erro ao enviar mensagem WhatsApp interna",
                    DataEnvio = DateTime.UtcNow,
                    Erro = ex.Message
                };
            }
        }

        /// <summary>
        /// Envia mensagem com mídia usando credenciais internas predefinidas
        /// </summary>
        public async Task<EnvioResponse> EnviarMidiaAsync(
            string numeroDestino,
            string mensagem,
            string urlMidia,
            TipoMensagemWhatsApp tipo)
        {
            try
            {
                _logger.LogInformation(
                    "Enviando mensagem WhatsApp interna com mídia - Para: {Numero}, Tipo: {Tipo}",
                    numeroDestino,
                    tipo
                );

                var resultado = await _whatsAppService.EnviarMidiaAsync(
                    numeroDestino: numeroDestino,
                    mensagem: mensagem,
                    urlMidia: urlMidia,
                    session: INSTANCE_DEFAULT,
                    tipo: tipo,
                    wahaApiUrl: ENDPOINT,
                    wahaApiKey: API_KEY
                );

                if (resultado.Sucesso)
                {
                    _logger.LogInformation(
                        "Mensagem WhatsApp interna com mídia enviada com sucesso - Para: {Numero}",
                        numeroDestino
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Falha ao enviar mensagem WhatsApp interna com mídia - Para: {Numero}, Erro: {Erro}",
                        numeroDestino,
                        resultado.Erro
                    );
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem WhatsApp interna com mídia - Para: {Numero}", numeroDestino);

                return new EnvioResponse
                {
                    Sucesso = false,
                    Mensagem = "Erro ao enviar mensagem WhatsApp interna com mídia",
                    DataEnvio = DateTime.UtcNow,
                    Erro = ex.Message
                };
            }
        }
    }
}
