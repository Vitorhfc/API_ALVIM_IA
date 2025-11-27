using Client_Service.ServiceGenerico.Interface;
using Shared.Classes.Entidades.Client;

namespace Client_Service.Service.Interface
{
    public interface IAgendamentoService : IServiceGenerico<Agendamento>
    {
        /// <summary>
        /// Busca agendamentos de um mês específico
        /// </summary>
        /// <param name="data">Data de referência (qualquer dia do mês)</param>
        /// <returns>Lista de agendamentos do mês</returns>
        Task<IEnumerable<Agendamento>> BuscarAgendamentosPorMesAsync(DateTime data);

        /// <summary>
        /// Busca agendamentos de um dia específico
        /// </summary>
        /// <param name="data">Data do dia</param>
        /// <returns>Lista de agendamentos do dia</returns>
        Task<IEnumerable<Agendamento>> BuscarAgendamentosPorDiaAsync(DateTime data);

        /// <summary>
        /// Busca agendamentos de um funcionário em um período
        /// </summary>
        /// <param name="funcionarioId">ID do funcionário</param>
        /// <param name="dataInicio">Data de início</param>
        /// <param name="dataFim">Data de fim</param>
        /// <returns>Lista de agendamentos do funcionário no período</returns>
        Task<IEnumerable<Agendamento>> BuscarAgendamentosPorFuncionarioAsync(string funcionarioId, DateTime dataInicio, DateTime dataFim);

        /// <summary>
        /// Cadastra ou edita um agendamento com validações
        /// </summary>
        /// <param name="agendamento">Agendamento a ser salvo</param>
        /// <returns>Agendamento salvo</returns>
        Task<Agendamento> CadastrarOuEditarAgendamentoAsync(Agendamento agendamento);

        /// <summary>
        /// Cancela um agendamento
        /// </summary>
        /// <param name="agendamentoId">ID do agendamento</param>
        /// <param name="motivo">Motivo do cancelamento</param>
        /// <returns>Agendamento cancelado</returns>
        Task<Agendamento> CancelarAgendamentoAsync(string agendamentoId, string motivo);

        /// <summary>
        /// Confirma um agendamento
        /// </summary>
        /// <param name="agendamentoId">ID do agendamento</param>
        /// <returns>Agendamento confirmado</returns>
        Task<Agendamento> ConfirmarAgendamentoAsync(string agendamentoId);

        /// <summary>
        /// Inicia atendimento de um agendamento
        /// </summary>
        /// <param name="agendamentoId">ID do agendamento</param>
        /// <returns>Agendamento em atendimento</returns>
        Task<Agendamento> IniciarAtendimentoAsync(string agendamentoId);

        /// <summary>
        /// Conclui um agendamento
        /// </summary>
        /// <param name="agendamentoId">ID do agendamento</param>
        /// <param name="observacoes">Observações finais</param>
        /// <returns>Agendamento concluído</returns>
        Task<Agendamento> ConcluirAgendamentoAsync(string agendamentoId, string? observacoes = null);

        /// <summary>
        /// Verifica se existe conflito de horário para os funcionários
        /// </summary>
        /// <param name="funcionarioIds">IDs dos funcionários</param>
        /// <param name="dataHoraInicio">Data/hora de início</param>
        /// <param name="dataHoraFim">Data/hora de fim</param>
        /// <param name="agendamentoIdExcluir">ID do agendamento a excluir da verificação</param>
        /// <returns>Lista de IDs de funcionários com conflito</returns>
        Task<List<string>> VerificarConflitoHorarioAsync(List<string> funcionarioIds, DateTime dataHoraInicio, DateTime dataHoraFim, string? agendamentoIdExcluir = null);
    }
}
