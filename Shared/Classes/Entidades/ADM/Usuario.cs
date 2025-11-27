using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.ADM
{
    public class Usuario : BaseEntidade
    {
        [BsonElement("nome")]
        public string Nome { get; set; } = "";

        [BsonElement("email")]
        public string Email { get; set; } = "";

        [BsonElement("cpf")]
        public string Cpf { get; set; } = "";

        [BsonElement("celular")]
        public string? Celular { get; set; } = "";

        [BsonElement("senha")]
        public string Senha { get; set; } = "";

        [BsonElement("dtaNascimento")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime DtaNascimento { get; set; }

        [BsonElement("dtaUltimoAcesso")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DtaUltimoAcesso { get; set; }

        [BsonElement("flgAutenticacaoDuasEtapas")]
        public bool FlgAutenticacaoDuasEtapas { get; set; }

        [BsonElement("flgInterno")]
        public bool FlgInterno { get; set; } = false;

        #region Token Email
        [BsonElement("tokenEmailConfirmacao")]
        public string? TokenEmailConfirmacao { get; set; } = "";

        [BsonElement("dtaTokenEmailGerado")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DtaTokenEmailGerado { get; set; }
        #endregion

        #region Token Celular
        [BsonElement("tokenCelularConfirmacao")]
        public string? TokenCelularConfirmacao { get; set; } = "";

        [BsonElement("dtaTokenCelularGerado")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DtaTokenCelularGerado { get; set; }
        #endregion

        #region Token Geral
        [BsonElement("tokenAcesso")]
        public string TokenAcesso { get; set; } = "";

        [BsonElement("dtaTokenAcessoGerado")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DtaTokenAcessoGerado { get; set; }

        [BsonElement("dtaTokenUtilizado")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DtaTokenUtilizado { get; set; }
        #endregion

        [BsonElement("tokenGoogle")]
        public string? TokenGoogle { get; set; }

        [BsonElement("flgEmpresaPier")]
        public bool FlgEmpresaPier { get; set; }
    }
}