using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.Client
{
    public class LogClient : BaseEntidade
    {
        [BsonElement("usuarioId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UsuarioId { get; set; }

        [BsonElement("empresaId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string EmpresaId { get; set; }

        [BsonElement("tipo")]
        public TipoLog Tipo { get; set; }

        [BsonElement("origem")]
        public string Origem { get; set; }

        [BsonElement("acao")]
        public string Acao { get; set; }

        [BsonElement("endpoint")]
        public string Endpoint { get; set; }

        [BsonElement("metodoHttp")]
        public string MetodoHttp { get; set; }

        [BsonElement("controller")]
        public string Controller { get; set; }

        [BsonElement("metodo")]
        public string Metodo { get; set; }

        [BsonElement("mensagem")]
        public string Mensagem { get; set; }

        [BsonElement("dadoAntigo")]
        public string DadoAntigo { get; set; }

        [BsonElement("dadoNovo")]
        public string DadoNovo { get; set; }

        [BsonElement("descricao")]
        public string Descricao { get; set; }

        [BsonElement("sucesso")]
        public bool Sucesso { get; set; }

        [BsonElement("ipAddress")]
        public string IpAddress { get; set; }

        [BsonElement("userAgent")]
        public string UserAgent { get; set; }

        [BsonElement("nivelSeveridade")]
        public NivelSeveridade NivelSeveridade { get; set; }

    }

    public enum TipoLog
    {
        Sistema = 1,
        Integracao = 2,
        Processamento = 3,
        Erro = 4,
        Webhook = 5
    }

    public enum NivelSeveridade
    {
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
}