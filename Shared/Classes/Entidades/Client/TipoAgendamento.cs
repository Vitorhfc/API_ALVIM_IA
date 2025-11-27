namespace Shared.Classes.Entidades.Client
{
    /// <summary>
    /// Tipos de agendamento disponíveis no sistema
    /// </summary>
    public enum TipoAgendamento
    {
        /// <summary>
        /// Consulta geral
        /// </summary>
        Consulta = 1,

        /// <summary>
        /// Avaliação inicial
        /// </summary>
        Avaliacao = 2,

        /// <summary>
        /// Retorno
        /// </summary>
        Retorno = 3,

        /// <summary>
        /// Procedimento
        /// </summary>
        Procedimento = 4,

        /// <summary>
        /// Exame
        /// </summary>
        Exame = 5,

        /// <summary>
        /// Reunião
        /// </summary>
        Reuniao = 6,

        /// <summary>
        /// Atendimento
        /// </summary>
        Atendimento = 7,

        /// <summary>
        /// Outro tipo de agendamento
        /// </summary>
        Outro = 99
    }
}
