using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.ADM
{
    public class RefreshToken : BaseEntidade
    {
        [BsonElement("usuarioId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UsuarioId { get; set; }

        [BsonElement("empresaId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string EmpresaId { get; set; }

        [BsonElement("token")]
        public string Token { get; set; }

        [BsonElement("dtaExpiracao")]
        public DateTime DtaExpiracao { get; set; }

        [BsonElement("dtaUso")]
        public DateTime? DtaUso { get; set; }

        [BsonElement("flgRevogado")]
        public bool FlgRevogado { get; set; }

        [BsonElement("flgUtilizado")]
        public bool FlgUtilizado { get; set; }

        [BsonElement("ipOrigem")]
        public string? IpOrigem { get; set; }

        [BsonElement("userAgent")]
        public string? UserAgent { get; set; }
    }
}