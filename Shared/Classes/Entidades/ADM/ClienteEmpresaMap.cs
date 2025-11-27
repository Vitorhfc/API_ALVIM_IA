using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.ADM
{
    /// <summary>
    /// Mapeamento entre Cliente e Empresa armazenado no banco Admin
    /// Permite buscar a empresa de um cliente sem precisar acessar o banco tenant
    /// </summary>
    public class ClienteEmpresaMap : BaseEntidade
    {
        /// <summary>
        /// ID do Cliente (do banco tenant)
        /// </summary>
        [BsonElement("clienteId")]
        public string ClienteId { get; set; } = string.Empty;

        /// <summary>
        /// ID da Empresa (do banco admin)
        /// </summary>
        [BsonElement("empresaId")]
        public string EmpresaId { get; set; } = string.Empty;

        /// <summary>
        /// Número do WhatsApp do cliente (para busca alternativa)
        /// </summary>
        [BsonElement("numeroWhatsApp")]
        public string? NumeroWhatsApp { get; set; }

        /// <summary>
        /// Nome do cliente (para debug/logs)
        /// </summary>
        [BsonElement("nomeCliente")]
        public string? NomeCliente { get; set; }

        /// <summary>
        /// Data da última atualização do mapeamento
        /// </summary>
        [BsonElement("dtaUltimaAtualizacao")]
        public DateTime DtaUltimaAtualizacao { get; set; } = DateTime.UtcNow;
    }
}
