namespace Shared.Classes.Model
{
    /// <summary>
    /// Request para envio de email
    /// </summary>
    public class EnviarEmailRequest
    {
        public List<string> Destinatarios { get; set; } = new();
        public string Assunto { get; set; }
        public string Corpo { get; set; }
        public bool CorpoHTML { get; set; } = true;
        public List<string>? DestinatariosCopiaOculta { get; set; }
        public string? EmailRemetente { get; set; } // Se null, usa padrão
        public string? NomeRemetente { get; set; } // Se null, usa padrão
    }

    /// <summary>
    /// Request para envio de WhatsApp
    /// </summary>
    public class EnviarWhatsAppRequest
    {
        /// <summary>
        /// Número de destino (com ou sem @c.us)
        /// </summary>
        public string NumeroDestino { get; set; } = string.Empty;

        /// <summary>
        /// Texto da mensagem
        /// </summary>
        public string Mensagem { get; set; } = string.Empty;

        /// <summary>
        /// Nome da sessão WAHA
        /// </summary>
        public string Session { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de mensagem (Texto, Imagem, Audio, Video, Documento)
        /// </summary>
        public TipoMensagemWhatsApp Tipo { get; set; } = TipoMensagemWhatsApp.Texto;

        /// <summary>
        /// URL da mídia (para mensagens de mídia)
        /// </summary>
        public string? UrlMidia { get; set; }

        /// <summary>
        /// Nome do arquivo (opcional)
        /// </summary>
        public string? NomeArquivo { get; set; }

        /// <summary>
        /// ID da mensagem a ser citada (reply)
        /// </summary>
        public string? QuotedMessageId { get; set; }
    }

    /// <summary>
    /// Response genérico para envios
    /// </summary>
    public class EnvioResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; }
        public string? IdEnvio { get; set; }
        public DateTime DataEnvio { get; set; }
        public string? Erro { get; set; }
    }

    /// <summary>
    /// Tipos de mensagem WhatsApp
    /// </summary>
    public enum TipoMensagemWhatsApp
    {
        Texto = 1,
        Imagem = 2,
        Audio = 3,
        Video = 4,
        Documento = 5
    }

    /// <summary>
    /// Response da criação de instância WAHA
    /// </summary>
    public class WAHAInstanceResponse
    {
        public bool Sucesso { get; set; }
        public string? Mensagem { get; set; }
        public string? InstanceName { get; set; }
        public string? Status { get; set; }
        public string? Erro { get; set; }
    }

    ///// <summary>
    ///// Response do status da instância WAHA
    ///// </summary>
    //public class WAHAStatusResponse
    //{
    //    public bool Sucesso { get; set; }
    //    public string? Status { get; set; } // CONNECTED, DISCONNECTED, SCAN_QR_CODE, etc
    //    public bool IsConnected { get; set; }
    //    public string? Mensagem { get; set; }
    //    public string? Erro { get; set; }
    //}

    /// <summary>
    /// Response do QR Code WAHA
    /// </summary>
    public class WAHAQrCodeResponse
    {
        public bool Sucesso { get; set; }
        public string? QrCodeBase64 { get; set; }
        public string? Status { get; set; }
        public string? Mensagem { get; set; }
        public string? Erro { get; set; }
    }

    /// <summary>
    /// Configurações da API WAHA do appsettings.json
    /// </summary>
    public class WAHASettings
    {
        public string ApiUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Informações de uma sessão WAHA
    /// </summary>
    //public class WAHASessionInfo
    //{
    //    public string Name { get; set; } = string.Empty;
    //    public string Status { get; set; } = string.Empty;
    //    public string? Config { get; set; }
    //    public string? Me { get; set; }
    //}

    /// <summary>
    /// Response da API WAHA para validação de número
    /// </summary>
    public class WhatsappNumbersResponse
    {
        public bool numberExists { get; set; }
        public string chatId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response para envio de mensagem
    /// </summary>
    public class SendMessageResponse
    {
        public SendMessageData _data { get; set; } = new();
        public string? error { get; set; }
    }

    public class SendMessageData
    {
        public MessageInfo Info { get; set; } = new();
    }

    public class MessageInfo
    {
        public string ID { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response para envio de áudio
    /// </summary>
    public class SendAudioResponse
    {
        public SendMessageData _data { get; set; } = new();
        public string? error { get; set; }
    }

    /// <summary>
    /// Response para envio de mídia
    /// </summary>
    public class SendMediaResponse
    {
        public SendMessageData _data { get; set; } = new();
        public string? error { get; set; }
    }

    /// <summary>
    /// Resultado da validação de número
    /// </summary>
    public class FetchNumberIdResult
    {
        public string Status { get; set; } = string.Empty;
        public bool NumberExists { get; set; }
        public string NumberId { get; set; } = string.Empty;

        public FetchNumberIdResult(string status, bool numberExists = false, string numberId = "")
        {
            Status = status;
            NumberExists = numberExists;
            NumberId = numberId;
        }

        public EnumPhoneIdResponse EnumPhoneIdResponse
        {
            get
            {
                if (Status == "success" && NumberExists)
                    return EnumPhoneIdResponse.registred;
                if (Status == "success" && !NumberExists)
                    return EnumPhoneIdResponse.notRegistered;
                if (Status == "error")
                    return EnumPhoneIdResponse.notConnected;
                return EnumPhoneIdResponse.error;
            }
        }
    }

    /// <summary>
    /// Enum para status de validação de telefone
    /// </summary>
    public enum EnumPhoneIdResponse
    {
        registred,
        notRegistered,
        notConnected,
        error
    }

    /// <summary>
    /// Enum para resultado de envio
    /// </summary>
    public enum EnumSentResult
    {
        success,
        invalidPhone,
        instanceError,
        requestError
    }

    /// <summary>
    /// Resultado de envio de mensagem
    /// </summary>
    public class SendMessageResult
    {
        public bool Success { get; set; }
        public EnumSentResult SentResult { get; set; }
        public string? MessageId { get; set; }

        public SendMessageResult(string messageId)
        {
            Success = true;
            SentResult = EnumSentResult.success;
            MessageId = messageId;
        }

        public SendMessageResult(bool success, EnumSentResult sentResult)
        {
            Success = success;
            SentResult = sentResult;
        }
    }

    #region Configuração WAHA

    /// <summary>
    /// Request para configurar instância WAHA de uma empresa
    /// </summary>
    public class ConfigurarInstanciaWahaRequest
    {
        public string NumeroWhatsApp { get; set; } = string.Empty; // Formato: 5541999887766
        public int EmpresaId { get; set; }
        public string? NomeInstancia { get; set; } // Se null, gera automaticamente
        public Dictionary<string, string>? Metadata { get; set; } // Metadados personalizados
    }

    /// <summary>
    /// Response da configuração de instância WAHA
    /// </summary>
    public class ConfigurarInstanciaWahaResponse
    {
        public bool Sucesso { get; set; }
        public string? Mensagem { get; set; }
        public string? NomeInstancia { get; set; }
        public string? StatusSessao { get; set; }
        public string? QrCodeBase64 { get; set; }
        public string? WebhookUrl { get; set; }
        public string? Erro { get; set; }
    }

    /// <summary>
    /// Request para criar sessão WAHA (estrutura da API)
    /// </summary>
    public class CreateWahaSessionRequest
    {
        public string name { get; set; } = string.Empty;
        public bool start { get; set; } = true;
        public WahaSessionConfig? config { get; set; }
    }

    /// <summary>
    /// Configuração da sessão WAHA
    /// </summary>
    public class WahaSessionConfig
    {
        public Dictionary<string, string>? metadata { get; set; }
        public List<WahaWebhookConfig>? webhooks { get; set; }
    }

    /// <summary>
    /// Configuração de webhook WAHA
    /// </summary>
    public class WahaWebhookConfig
    {
        public string url { get; set; } = string.Empty;
        public List<string> events { get; set; } = new();
        public string? hmac { get; set; }
        public int? retries { get; set; }
        public Dictionary<string, string>? customHeaders { get; set; }
    }

    /// <summary>
    /// Response da API WAHA ao criar sessão
    /// </summary>
    public class CreateWahaSessionResponse
    {
        public string? name { get; set; }
        public string? status { get; set; }
        public WahaSessionConfig? config { get; set; }
        public WahaSessionMe? me { get; set; }
    }

    /// <summary>
    /// Informações do usuário conectado na sessão
    /// </summary>
    public class WahaSessionMe
    {
        public string? id { get; set; }
        public string? pushName { get; set; }
    }

    #endregion
}
