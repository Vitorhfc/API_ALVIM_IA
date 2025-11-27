using Newtonsoft.Json;

namespace Shared.Classes.Model
{
    public class ProcessamentoWebhookResult
    {
        public bool Sucesso { get; set; }
        public string? IdProcessamento { get; set; }
        public string? Erro { get; set; }
    }

    public class ValidacaoWebhookResult
    {
        public bool EhValido { get; set; }
        public bool DeveIgnorar { get; set; }
        public string? TipoErro { get; set; }
        public object? MensagemErro { get; set; }
        public WebhookWaHaRequest? WebhookRequest { get; set; }
    }

    public class WebhookWaHaRequest
    {
        public string? id { get; set; }
        public long? timestamp { get; set; } // ✅ Nullable
        public string? session { get; set; }
        public Metadata? metadata { get; set; }
        public string? engine { get; set; }
        public string? @event { get; set; }
        public Payload? payload { get; set; }
        public Me? me { get; set; }
        public Environment? environment { get; set; }

        public class Metadata
        {
            [JsonProperty("user.id")]
            public string? userId { get; set; }
            [JsonProperty("user.email")]
            public string? userEmail { get; set; }
        }

        public class Payload
        {
            public string? id { get; set; }
            public long? timestamp { get; set; } // ✅ Nullable
            public string? from { get; set; }
            public bool fromMe { get; set; }
            public string? source { get; set; }
            public string? to { get; set; }
            public string? me { get; set; } // ✅ Adicionado
            public string? participant { get; set; }
            public string? body { get; set; }
            public bool hasMedia { get; set; }
            public Media? media { get; set; }
            public int? ack { get; set; } // ✅ Nullable
            public string? ackName { get; set; }
            public string? author { get; set; }
            public string? notifyName { get; set; } // ✅ Adicionado
            public string? type { get; set; }
            public Location? location { get; set; }
            public List<string>? vCards { get; set; }

            [JsonProperty("_data")]
            public PayloadData? data { get; set; }

            public ReplyTo? replyTo { get; set; }
            public string? editedMessageId { get; set; }
            public string? revokedMessageId { get; set; }
            public After? after { get; set; }
            public Before? before { get; set; }
            public Group? group { get; set; }
            public List<Participant>? participants { get; set; }
            public string? name { get; set; }
            public string? status { get; set; }
        }

        public class Media
        {
            public string? url { get; set; }
            public string? mimetype { get; set; }
            public string? filename { get; set; }
            public S3? s3 { get; set; }
            public object? error { get; set; }
        }

        public class S3
        {
            public string? Bucket { get; set; }
            public string? Key { get; set; }
        }

        public class Location
        {
            public string? description { get; set; }
            public string? latitude { get; set; }
            public string? longitude { get; set; }
        }

        public class ReplyTo
        {
            public string? id { get; set; }
            public string? participant { get; set; }
            public string? body { get; set; }

            [JsonProperty("_data")]
            public PayloadData? data { get; set; }
        }

        public class After
        {
            public string? id { get; set; }
            public long? timestamp { get; set; }
            public string? from { get; set; }
            public bool fromMe { get; set; }
            public string? source { get; set; }
            public string? to { get; set; }
            public string? participant { get; set; }
            public string? body { get; set; }
            public bool hasMedia { get; set; }
            public Media? media { get; set; }
            public int? ack { get; set; }
            public string? ackName { get; set; }
            public string? author { get; set; }
            public Location? location { get; set; }
            public List<string>? vCards { get; set; }

            [JsonProperty("_data")]
            public PayloadData? data { get; set; }

            public ReplyTo? replyTo { get; set; }
        }

        public class Before
        {
            public string? id { get; set; }
            public long? timestamp { get; set; }
            public string? from { get; set; }
            public bool fromMe { get; set; }
            public string? source { get; set; }
            public string? to { get; set; }
            public string? participant { get; set; }
            public string? body { get; set; }
            public bool hasMedia { get; set; }
            public Media? media { get; set; }
            public int? ack { get; set; }
            public string? ackName { get; set; }
            public string? author { get; set; }
            public Location? location { get; set; }
            public List<string>? vCards { get; set; }

            [JsonProperty("_data")]
            public PayloadData? data { get; set; }

            public ReplyTo? replyTo { get; set; }
        }

        public class Group
        {
            public string? id { get; set; }
        }

        public class Participant
        {
            public string? id { get; set; }
            public string? role { get; set; }
        }

        public class Me
        {
            public string? id { get; set; }
            public string? pushName { get; set; }
        }

        public class Environment
        {
            public string? version { get; set; }
            public string? engine { get; set; }
            public string? tier { get; set; }
            public string? browser { get; set; }
        }

        public class PayloadData
        {
            public Info? Info { get; set; }
            public Message? Message { get; set; }
            public IList<string>? messageIds { get; set; }
        }

        public class Info
        {
            public string? Chat { get; set; }
            public string? Sender { get; set; }
            public bool IsFromMe { get; set; }
            public bool IsGroup { get; set; }
            public string? AddressingMode { get; set; }
            public string? SenderAlt { get; set; }
            public string? RecipientAlt { get; set; }
            public string? BroadcastListOwner { get; set; }
            public string? ID { get; set; }
            public int ServerID { get; set; }
            public string? Type { get; set; }
            public string? PushName { get; set; }
            public string? Timestamp { get; set; }
            public string? Category { get; set; }
            public bool Multicast { get; set; }
            public string? MediaType { get; set; }
            public string? Edit { get; set; }
            public MsgBotInfo? MsgBotInfo { get; set; }
            public MsgMetaInfo? MsgMetaInfo { get; set; }

            public string GetSenderNumber
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(Sender))
                        return string.Empty;

                    var withoutDomain = Sender.Split('@')[0];
                    var numberOnly = withoutDomain.Split(':')[0];
                    return numberOnly;
                }
            }
        }

        public class Message
        {
            public ContactsArrayMessage? contactsArrayMessage { get; set; }
            public Contacts? contactMessage { get; set; }
            public TemplateMessage? templateMessage { get; set; }
        }

        public class ContactsArrayMessage
        {
            public IList<Contacts>? contacts { get; set; }
        }

        public class Contacts
        {
            public string? displayName { get; set; }
            public string? vcard { get; set; }
        }

        public class MsgBotInfo
        {
            public string? EditType { get; set; }
            public string? EditTargetID { get; set; }
            public string? EditSenderTimestampMS { get; set; }
        }

        public class MsgMetaInfo
        {
            public string? TargetID { get; set; }
            public string? TargetSender { get; set; }
            public string? TargetChat { get; set; }
            public object? DeprecatedLIDSession { get; set; }
            public string? ThreadMessageID { get; set; }
            public string? ThreadMessageSenderJID { get; set; }
        }

        public class TemplateMessage
        {
            public Format? Format { get; set; }
            public string? templateID { get; set; }
        }

        public class Format
        {
            public object? InteractiveMessageTemplate { get; set; }
        }
    }
}