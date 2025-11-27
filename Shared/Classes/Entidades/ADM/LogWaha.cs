using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.ADM
{
    public class LogWaha : BaseEntidade
    {
        [BsonElement("idLog")]
        public string IdLog { get; set; } = string.Empty;

        [BsonElement("empresaId")]
        public string? EmpresaId { get; set; }

        [BsonElement("tipoEvento")]
        public string TipoEvento { get; set; } = string.Empty;

        [BsonElement("origem")]
        public string Origem { get; set; } = string.Empty;

        [BsonElement("acao")]
        public string Acao { get; set; } = string.Empty;

        [BsonElement("payload")]
        public string Payload { get; set; } = string.Empty;

        [BsonElement("payloadLimpo")]
        public string? PayloadLimpo { get; set; }

        [BsonElement("sucesso")]
        public bool Sucesso { get; set; } = true;

        [BsonElement("mensagemErro")]
        public string? MensagemErro { get; set; }

        [BsonElement("stackTrace")]
        public string? StackTrace { get; set; }

        [BsonElement("sessionId")]
        public string? SessionId { get; set; }

        [BsonElement("telefoneOrigem")]
        public string? TelefoneOrigem { get; set; }

        [BsonElement("telefoneDestino")]
        public string? TelefoneDestino { get; set; }

        [BsonElement("messageId")]
        public string? MessageId { get; set; }

        [BsonElement("ipAddress")]
        public string? IpAddress { get; set; }

        [BsonElement("userAgent")]
        public string? UserAgent { get; set; }

        [BsonElement("tempoProcessamento")]
        public long? TempoProcessamentoMs { get; set; }

        [BsonElement("statusHttp")]
        public int? StatusHttp { get; set; }

        [BsonElement("metadados")]
        public Dictionary<string, string>? Metadados { get; set; }
    }
}
