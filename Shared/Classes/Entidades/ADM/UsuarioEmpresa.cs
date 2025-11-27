using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.ADM
{
    public class UsuarioEmpresa : BaseEntidade
    {
        [BsonElement("usuarioId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UsuarioId { get; set; }

        [BsonElement("empresaId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string EmpresaId { get; set; }

        [BsonElement("flgAdministrador")]
        public bool FlgAdministrador { get; set; }

        [BsonElement("dtaVinculo")]
        public DateTime DtaVinculo { get; set; }

        [BsonElement("flgEmpresaPadrao")]
        public bool FlgEmpresaPadrao { get; set; } = false;
    }
}
