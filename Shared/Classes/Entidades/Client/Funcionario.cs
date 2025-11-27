using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.Client
{
    public class Funcionario : BaseEntidade
    {
        [BsonElement("usuarioId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UsuarioId { get; set; } = "";

        [BsonElement("cargo")]
        public string? Cargo { get; set; } = "";

        [BsonElement("departamento")]
        public string? Departamento { get; set; } = "";

        [BsonElement("dtaAdmissao")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DtaAdmissao { get; set; }

        [BsonElement("horariosAtendimento")]
        public List<HorarioAtendimento> HorariosAtendimento { get; set; } = new List<HorarioAtendimento>();
    }

    public class HorarioAtendimento
    {
        [BsonElement("diaSemana")]
        public DiaSemana DiaSemana { get; set; }

        [BsonElement("horaInicio")]
        public TimeSpan HoraInicio { get; set; }

        [BsonElement("horaFim")]
        public TimeSpan HoraFim { get; set; }

        [BsonElement("flgAtivo")]
        public bool FlgAtivo { get; set; } = true;
    }

    public enum DiaSemana
    {
        Domingo = 0,
        Segunda = 1,
        Terca = 2,
        Quarta = 3,
        Quinta = 4,
        Sexta = 5,
        Sabado = 6
    }
}
