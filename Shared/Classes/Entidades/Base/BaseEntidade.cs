using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Shared.Classes.Entidades.Base
{
    public class BaseEntidade
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = "";

        [BsonElement("dtaCadastro")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DtaCadastro { get; set; } = DateTime.Now;

        [BsonElement("dtaAlteracao")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DtaAlteracao { get; set; } = DateTime.Now;

        [BsonElement("flgAtivo")]
        public bool FlgAtivo { get; set; } = true;
    }
}
