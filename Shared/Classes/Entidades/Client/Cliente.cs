using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;
using System.ComponentModel.DataAnnotations;

namespace Shared.Classes.Entidades.Client
{
    public class Cliente : BaseEntidade
    {
        [BsonElement("nome")]
        public string Nome { get; set; } = "";

        [BsonElement("numero")]
        public string Numero { get; set; } = "";

        [BsonElement("numeroTelefoneWaha")]
        public string NumeroTelefoneWaha { get; set; } = "";

        [BsonElement("numeroInterno")]
        public string NumeroInterno { get; set; } = "";

        [BsonElement("fotoPerfilUrl")]
        public string? FotoPerfilUrl { get; set; } = null;

        /// <summary>
        /// Nome do perfil do WhatsApp (pushname)
        /// </summary>
        [BsonElement("pushName")]
        public string? PushName { get; set; } = null;

        /// <summary>
        /// ID completo do WhatsApp (ex: 5512988505282@c.us)
        /// </summary>
        [BsonElement("whatsAppId")]
        public string? WhatsAppId { get; set; } = null;

        /// <summary>
        /// Se o contato está salvo nos contatos do WhatsApp
        /// </summary>
        [BsonElement("isMyContact")]
        public bool? IsMyContact { get; set; } = null;

        /// <summary>
        /// Se é um contato válido do WhatsApp
        /// </summary>
        [BsonElement("isWAContact")]
        public bool? IsWAContact { get; set; } = null;

        /// <summary>
        /// Última vez que as informações do WhatsApp foram atualizadas
        /// </summary>
        [BsonElement("dtUltimaAtualizacaoWaha")]
        public DateTime? DtUltimaAtualizacaoWaha { get; set; } = null;

        [BsonElement("email")]
        public string Email { get; set; } = "";

        [BsonElement("cpf")]
        public string Cpf { get; set; } = "";

        [BsonElement("statusConversa")]
        public StatusConversa StatusConversa { get; set; } = StatusConversa.Ativa;

        [BsonElement("dtPrimeiroContato")]
        public DateTime? DtPrimeiroContato { get; set; } = null;

        [BsonElement("dtUltimaInteracao")]
        public DateTime? DtUltimaInteracao { get; set; } = null;

        [BsonElement("dtFinalizacaoConversa")]
        public DateTime? DtFinalizacaoConversa { get; set; } = null;

        [BsonElement("funcionarioResponsavelId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? FuncionarioResponsavelId { get; set; } = null;

        [BsonElement("totalMensagens")]
        public int TotalMensagens { get; set; } = 0;

        [BsonElement("contexto")]
        public ContextoAtual? Contexto { get; set; } = null;

        [BsonElement("flgRespostaResponsavel")]
        public bool FlgRespostaResponsavel { get; set; } = false;

        [BsonElement("dtFlgResponsavelAtiva")]
        public DateTime? DtFlgResponsavelAtiva { get; set; } = null;

        [BsonElement("dtFlgResponsavelDesativada")]
        public DateTime? DtFlgResponsavelDesativada { get; set; } = null;
    }

    public class ContextoAtual
    {
        [BsonElement("intencaoIdentificada")]
        public string? IntencaoIdentificada { get; set; } = "";

        [BsonElement("entidadesExtraidas")]
        public Dictionary<string, string>? EntidadesExtraidas { get; set; } = null;

        [BsonElement("proximaAcaoSugerida")]
        public string? ProximaAcaoSugerida { get; set; } = "";

        [BsonElement("dtUltimaAtualizacao")]
        public DateTime? DtUltimaAtualizacao { get; set; } = null;
    }

    public enum StatusConversa
    {
        Ativa = 1,
        EmAtendimentoHumano = 2,
        Finalizada = 3,
        Aguardando = 4
    }
}
