using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;
using Shared.Classes.Model;

namespace Shared.Classes.Entidades.Client
{
    public class Mensagem : BaseEntidade
    {
        [BsonElement("clienteId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ClienteId { get; set; }

        [BsonElement("idMensagemWhatsApp")]
        public string IdMensagemWhatsApp { get; set; }

        [BsonElement("tipoMensagem")]
        public TipoMensagem TipoMensagem { get; set; }

        [BsonElement("origem")]
        public OrigemMensagem Origem { get; set; }

        [BsonElement("flgMensagemCliente")]
        public bool FlgMensagemCliente { get; set; }

        [BsonElement("conteudoTexto")]
        public string ConteudoTexto { get; set; }

        [BsonElement("midia")]
        public MidiaInfo Midia { get; set; }

        [BsonElement("idMensagemResposta")]
        public string IdMensagemResposta { get; set; }

        [BsonElement("reacoes")]
        public List<Reacao> Reacoes { get; set; } = new();

        [BsonElement("dtRecebido")]
        public DateTime DtRecebido { get; set; }

        [BsonElement("dtProcessamento")]
        public DateTime? DtProcessamento { get; set; }

        [BsonElement("timestampWhatsApp")]
        public DateTime TimestampWhatsApp { get; set; }

        [BsonElement("statusEntrega")]
        public StatusEntrega StatusEntrega { get; set; }

        [BsonElement("flgEnviadoAoN8N")]
        public bool FlgEnviadaAoN8N { get; set; }

        [BsonElement("grupoProcessamentoId")]
        public string GrupoProcessamentoId { get; set; }

        [BsonElement("metadados")]
        public Dictionary<string, object> Metadados { get; set; }
    }

    public class Reacao
    {
        [BsonElement("emoji")]
        public string Emoji { get; set; }

        [BsonElement("dtReacao")]
        public DateTime DtReacao { get; set; }

        [BsonElement("flgEnviada")]
        public bool FlgEnviada { get; set; }

        [BsonElement("dtEnvio")]
        public DateTime? DtEnvio { get; set; }
    }

    public enum TipoMensagem
    {
        Texto = 1,
        Audio = 2,
        Imagem = 3,
        Video = 4,
        Documento = 5,
        Contato = 6,
        Localizacao = 7,
        Sticker = 8
    }

    public enum OrigemMensagem
    {
        Cliente = 1,
        IA = 2,
        Funcionario = 3
    }

    public enum StatusEntrega
    {
        Enviada = 1,
        Entregue = 2,
        Lida = 3,
        Falha = 4
    }
}
