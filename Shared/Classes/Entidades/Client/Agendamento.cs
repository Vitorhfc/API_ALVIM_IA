using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Classes.Entidades.Base;

namespace Shared.Classes.Entidades.Client
{
    public class Agendamento : BaseEntidade
    {
        [BsonElement("clienteId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ClienteId { get; set; } = "";

        [BsonElement("funcionarioIds")]
        public List<string> FuncionarioIds { get; set; } = new List<string>();

        [BsonElement("tipoAgendamento")]
        public TipoAgendamento TipoAgendamento { get; set; }

        [BsonElement("titulo")]
        public string Titulo { get; set; } = "";

        [BsonElement("descricao")]
        public string? Descricao { get; set; }

        [BsonElement("dataHoraInicio")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime DataHoraInicio { get; set; }

        [BsonElement("dataHoraFim")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime DataHoraFim { get; set; }

        [BsonElement("status")]
        public StatusAgendamento Status { get; set; } = StatusAgendamento.Agendado;

        [BsonElement("observacoes")]
        public string? Observacoes { get; set; }

        [BsonElement("localAtendimento")]
        public string? LocalAtendimento { get; set; }

        #region Campos para futura integração com Google Agenda

        [BsonElement("googleEventId")]
        public string? GoogleEventId { get; set; }

        [BsonElement("googleCalendarId")]
        public string? GoogleCalendarId { get; set; }

        [BsonElement("sincronizadoGoogleAgenda")]
        public bool SincronizadoGoogleAgenda { get; set; } = false;

        [BsonElement("dtaUltimaSincronizacao")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DtaUltimaSincronizacao { get; set; }

        #endregion

        [BsonElement("dtaCancelamento")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DtaCancelamento { get; set; }

        [BsonElement("motivoCancelamento")]
        public string? MotivoCancelamento { get; set; }

        [BsonElement("dtaConclusao")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DtaConclusao { get; set; }
    }

    public enum StatusAgendamento
    {
        /// <summary>
        /// Agendamento confirmado
        /// </summary>
        Agendado = 1,

        /// <summary>
        /// Agendamento confirmado pelo cliente
        /// </summary>
        Confirmado = 2,

        /// <summary>
        /// Em atendimento
        /// </summary>
        EmAtendimento = 3,

        /// <summary>
        /// Atendimento concluído
        /// </summary>
        Concluido = 4,

        /// <summary>
        /// Agendamento cancelado
        /// </summary>
        Cancelado = 5,

        /// <summary>
        /// Cliente não compareceu
        /// </summary>
        NaoCompareceu = 6,

        /// <summary>
        /// Reagendado
        /// </summary>
        Reagendado = 7
    }
}
