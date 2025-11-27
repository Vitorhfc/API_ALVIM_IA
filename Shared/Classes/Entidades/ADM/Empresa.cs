using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.ADM
{
    public class Empresa : BaseEntidade
    {
        /// <summary>
        /// Razão Social
        /// </summary>
        [BsonElement("razaoSocial")]
        public string RazaoSocial { get; set; } = string.Empty;

        /// <summary>
        /// Nome Fantasia
        /// </summary>
        [BsonElement("nomeFantasia")]
        public string? Nome { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        [BsonElement("email")]
        public string? Email { get; set; }

        /// <summary>
        /// CNPJ
        /// </summary>
        [BsonElement("cnpj")]
        public string CNPJ { get; set; } = string.Empty;

        /// <summary>
        /// Connection String do banco do tenant (criptografada)
        /// </summary>
        [BsonElement("connectionString")]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Nome do banco de dados do tenant
        /// </summary>
        [BsonElement("nomeBaseDados")]
        public string NomeBaseDados { get; set; } = string.Empty;

        #region Configurações WAHA

        /// <summary>
        /// Nome da sessão no WAHA (único por empresa, usado para enviar mensagens)
        /// Exemplo: "TesteVitinhoMuitoLoko", "Empresa-ABC-2025"
        /// Este é o identificador principal da empresa no WAHA
        /// </summary>
        [BsonElement("wahaSessionName")]
        public string? WahaSessionName { get; set; } = null;

        /// <summary>
        /// Número do WhatsApp da empresa (usado para identificar tenant)
        /// </summary>
        [BsonElement("wahaNumeroWhatsApp")]
        public string? WahaNumeroWhatsApp { get; set; } = null;

        /// <summary>
        /// Indica se o WAHA está ativo
        /// </summary>
        [BsonElement("flgWahaAtivo")]
        public bool FlgWahaAtivo { get; set; }

        /// <summary>
        /// Data da conexão do WAHA
        /// </summary>
        [BsonElement("wahaDataConexao")]
        public DateTime? WahaDataConexao { get; set; } = null;

        /// <summary>
        /// Data da última verificação do status WAHA
        /// </summary>
        [BsonElement("wahaUltimaVerificacao")]
        public DateTime? WahaUltimaVerificacao { get; set; } = null;

        #endregion


        /// <summary>
        /// Empresa suspensa (não processa webhooks)
        /// </summary>
        [BsonElement("flgSuspensa")]
        public bool FlgSuspensa { get; set; }

    }
}

