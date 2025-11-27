using System.Text.Json.Serialization;

namespace Shared.Classes.Model
{
    #region Request Models

    /// <summary>
    /// Request para criar/iniciar uma sessão WAHA
    /// </summary>
    public class WAHAIniciarSessaoRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("start")]
        public bool Start { get; set; } = true;

        [JsonPropertyName("config")]
        public WAHASessionConfig? Config { get; set; }
    }

    /// <summary>
    /// Configurações de uma sessão WAHA
    /// </summary>
    public class WAHASessionConfig
    {
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        [JsonPropertyName("proxy")]
        public string? Proxy { get; set; }

        [JsonPropertyName("debug")]
        public bool Debug { get; set; } = false;

        [JsonPropertyName("ignore")]
        public WAHAIgnoreConfig? Ignore { get; set; }

        [JsonPropertyName("noweb")]
        public WAHANowebConfig? Noweb { get; set; }

        [JsonPropertyName("webjs")]
        public WAHAWebjsConfig? Webjs { get; set; }

        [JsonPropertyName("webhooks")]
        public List<WAHAWebhookConfig>? Webhooks { get; set; }
    }

    /// <summary>
    /// Configuração de ignore (o que ignorar)
    /// </summary>
    public class WAHAIgnoreConfig
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; } = false;

        [JsonPropertyName("groups")]
        public bool Groups { get; set; } = false;

        [JsonPropertyName("channels")]
        public bool Channels { get; set; } = false;

        [JsonPropertyName("broadcast")]
        public bool Broadcast { get; set; } = true;
    }

    /// <summary>
    /// Configuração Noweb (headless)
    /// </summary>
    public class WAHANowebConfig
    {
        [JsonPropertyName("markOnline")]
        public bool MarkOnline { get; set; } = true;

        [JsonPropertyName("store")]
        public WAHAStoreConfig? Store { get; set; }
    }

    /// <summary>
    /// Configuração de store
    /// </summary>
    public class WAHAStoreConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("fullSync")]
        public bool FullSync { get; set; } = false;
    }

    /// <summary>
    /// Configuração Webjs
    /// </summary>
    public class WAHAWebjsConfig
    {
        [JsonPropertyName("tagsEventsOn")]
        public bool TagsEventsOn { get; set; } = false;
    }

    /// <summary>
    /// Configuração de webhook WAHA
    /// </summary>
    public class WAHAWebhookConfig
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("events")]
        public List<string> Events { get; set; } = new List<string>();

        [JsonPropertyName("hmac")]
        public WAHAHmacConfig? Hmac { get; set; }

        [JsonPropertyName("retries")]
        public WAHARetriesConfig? Retries { get; set; }

        [JsonPropertyName("customHeaders")]
        public Dictionary<string, string>? CustomHeaders { get; set; }
    }

    /// <summary>
    /// Configuração HMAC para webhook
    /// </summary>
    public class WAHAHmacConfig
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }
    }

    /// <summary>
    /// Configuração de retries para webhook
    /// </summary>
    public class WAHARetriesConfig
    {
        [JsonPropertyName("delaySeconds")]
        public int DelaySeconds { get; set; } = 1;

        [JsonPropertyName("attempts")]
        public int Attempts { get; set; } = 1;

        [JsonPropertyName("policy")]
        public string Policy { get; set; } = "Exponential";
    }

    #endregion

    #region Response Models

    /// <summary>
    /// Resposta com status da sessão WAHA
    /// </summary>
    public class WAHAStatusResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("me")]
        public WAHAMeInfo? Me { get; set; }

        [JsonPropertyName("engine")]
        public WAHAEngineInfo? Engine { get; set; }  // Alterado de string? para WAHAEngineInfo?

        [JsonPropertyName("config")]
        public WAHASessionConfig? Config { get; set; }

        // Propriedades adicionais
        public bool Sucesso { get; set; }
        public string? Mensagem { get; set; }
        public string? Erro { get; set; }
        public bool IsConnected { get; set; }

        // Propriedades calculadas
        public bool EstaConectado => Status == "WORKING" || State == "WORKING";
        public bool PrecisaQRCode => Status == "STARTING" || Status == "SCAN_QR_CODE";
    }

    /// <summary>
    /// Informações do engine WAHA
    /// </summary>
    public class WAHAEngineInfo
    {
        [JsonPropertyName("grpc")]
        public WAHAGrpcInfo? Grpc { get; set; }

        [JsonPropertyName("gows")]
        public WAHAGowsInfo? Gows { get; set; }
    }


    /// <summary>
    /// Informações do GRPC
    /// </summary>
    public class WAHAGrpcInfo
    {
        [JsonPropertyName("client")]
        public string? Client { get; set; }

        [JsonPropertyName("stream")]
        public string? Stream { get; set; }
    }

    /// <summary>
    /// Informações do GOWS
    /// </summary>
    public class WAHAGowsInfo
    {
        [JsonPropertyName("found")]
        public bool Found { get; set; }

        [JsonPropertyName("connected")]
        public bool Connected { get; set; }
    }

    /// <summary>
    /// Informações de uma sessão WAHA
    /// </summary>
    public class WAHASessionInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("me")]
        public WAHAMeInfo? Me { get; set; }

        [JsonPropertyName("config")]
        public WAHASessionConfig? Config { get; set; }

        [JsonPropertyName("engine")]
        public WAHAEngineInfo? Engine { get; set; }  // Alterado de string? para WAHAEngineInfo?

        public bool EstaConectado => Status == "WORKING" || State == "WORKING";
    }


    /// <summary>
    /// Resposta com QR Code da sessão WAHA
    /// </summary>
    public class WAHAQRCodeResponse
    {
        [JsonPropertyName("qr")]
        public string? QRCode { get; set; }

        [JsonPropertyName("qrImage")]
        public string? QRCodeImage { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        // Propriedades adicionais do WhatsAppService
        public bool Sucesso { get; set; }
        public string? QrCodeBase64 { get; set; }
        public string? Status { get; set; }
        public string? Erro { get; set; }
    }

    /// <summary>
    /// Informações do usuário conectado
    /// </summary>
    public class WAHAMeInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("pushName")]
        public string? PushName { get; set; }
    }

    /// <summary>
    /// Resposta padrão para ações WAHA
    /// </summary>
    public class WAHAActionResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        public bool Sucesso => Success;
    }


    /// <summary>
    /// Informações da conta conectada
    /// </summary>
    public class WAHAAccountInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("pushName")]
        public string? PushName { get; set; }

        [JsonPropertyName("platform")]
        public string? Platform { get; set; }

        [JsonPropertyName("wid")]
        public string? Wid { get; set; }

        public string NumeroFormatado => Id.Replace("@c.us", "").Replace("@s.whatsapp.net", "");
    }

    /// <summary>
    /// Informações de contato retornadas pela API do WAHA
    /// </summary>
    public class WahaContactInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("pushname")]
        public string? PushName { get; set; }

        [JsonPropertyName("shortName")]
        public string? ShortName { get; set; }

        [JsonPropertyName("isGroup")]
        public bool IsGroup { get; set; }

        [JsonPropertyName("isMe")]
        public bool IsMe { get; set; }

        [JsonPropertyName("isMyContact")]
        public bool IsMyContact { get; set; }

        [JsonPropertyName("isUser")]
        public bool IsUser { get; set; }

        [JsonPropertyName("isWAContact")]
        public bool IsWAContact { get; set; }

        [JsonPropertyName("profilePicThumbObj")]
        public WahaProfilePicture? ProfilePicThumbObj { get; set; }

        /// <summary>
        /// Nome mais adequado para exibição (prioriza Name, depois PushName, depois Number)
        /// </summary>
        [JsonIgnore]
        public string NomeExibicao => !string.IsNullOrWhiteSpace(Name) ? Name
                                    : !string.IsNullOrWhiteSpace(PushName) ? PushName
                                    : Number ?? "Desconhecido";
    }

    /// <summary>
    /// Informações da foto de perfil do contato
    /// </summary>
    public class WahaProfilePicture
    {
        [JsonPropertyName("eurl")]
        public string? Eurl { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("img")]
        public string? Img { get; set; }

        [JsonPropertyName("imgFull")]
        public string? ImgFull { get; set; }

        [JsonPropertyName("raw")]
        public string? Raw { get; set; }

        [JsonPropertyName("tag")]
        public string? Tag { get; set; }
    }

    #endregion

    #region Enums

    /// <summary>
    /// Status possíveis de uma sessão WAHA
    /// </summary>
    public static class WAHAStatusEnum
    {
        public const string STARTING = "STARTING";
        public const string SCAN_QR_CODE = "SCAN_QR_CODE";
        public const string WORKING = "WORKING";
        public const string FAILED = "FAILED";
        public const string STOPPED = "STOPPED";
    }

    /// <summary>
    /// Eventos de webhook WAHA
    /// </summary>
    public static class WAHAWebhookEvents
    {
        public const string MESSAGE = "message";
        public const string MESSAGE_ANY = "message.any";
        public const string STATE_CHANGE = "state.change";
        public const string SESSION_STATUS = "session.status";
        public const string MESSAGE_ACK = "message.ack";
        public const string MESSAGE_REVOKED = "message.revoked";
    }

    #endregion
}