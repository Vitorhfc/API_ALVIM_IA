using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Classes.Entidades.ADM
{
    public class LogADM : BaseEntidade
    {
        [BsonElement("usuarioId")]
        public string UsuarioId { get; set; } = "";

        [BsonElement("empresaId")]
        public string EmpresaId { get; set; } = "";

        [BsonElement("acao")]
        public string Acao { get; set; } = "";

        [BsonElement("sucesso")]
        public bool Sucesso { get; set; } = true;

        [BsonElement("descricao")]
        public string Descricao { get; set; } = "";

        [BsonElement("ipAddress")]
        public string IpAddress { get; set; } = "";

        [BsonElement("userAgent")]
        public string UserAgent { get; set; } = "";
    }
}
