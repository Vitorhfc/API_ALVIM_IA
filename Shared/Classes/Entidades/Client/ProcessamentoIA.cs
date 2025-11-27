using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;
using static Shared.Enumeradores.Enumeradores;

namespace Shared.Classes.Entidades.Client
{
    public class ProcessamentoIA : BaseEntidade
    {
        [BsonElement("clienteId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ClienteId { get; set; }

        [BsonElement("grupoProcessamentoId")]
        public string GrupoProcessamentoId { get; set; }

        [BsonElement("mensagensIds")]
        public List<string> MensagensIds { get; set; }

        [BsonElement("qtdMensagensProcessadas")]
        public int QtdMensagensProcessadas { get; set; }

        [BsonElement("flgPrimeiraMensagemDoDia")]
        public bool FlgPrimeiraMensagemDoDia { get; set; }

        [BsonElement("payloadEnviadoJson")]
        public string PayloadEnviadoJson { get; set; }

        [BsonElement("respostaCompletaJson")]
        public string RespostaCompletaJson { get; set; }

        [BsonElement("qtdMensagensEnviadas")]
        public int QtdMensagensEnviadas { get; set; }

        [BsonElement("qtdReacoesEnviadas")]
        public int QtdReacoesEnviadas { get; set; }

        [BsonElement("documentosEnviados")]
        public List<string> DocumentosEnviados { get; set; } = new();

        [BsonElement("tipoEsclarecimento")]
        public string TipoEsclarecimento { get; set; }

        [BsonElement("status")]
        public StatusProcessamento Status { get; set; }

        [BsonElement("erroDescricao")]
        public string ErroDescricao { get; set; }

        [BsonElement("dtProcessamento")]
        public DateTime DtProcessamento { get; set; }
    }

    public enum StatusProcessamento
    {
        Sucesso = 1,
        Erro = 2,
        Timeout = 3
    }
}
