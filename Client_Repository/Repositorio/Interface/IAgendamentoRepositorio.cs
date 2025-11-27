using Client_Repository.RepositorioGenerico.Interface;
using Shared.Classes.Entidades.Client;

namespace Client_Repository.Repositorio.Interface
{
    /// <summary>
    /// Interface do repositório de Agendamento - Métodos Adicionais
    /// </summary>
    public interface IAgendamentoRepositorio : IRepositorioGenerico<Agendamento>
    {
        /// <summary>
        /// Busca agendamentos de um mês específico
        /// </summary>
        /// <param name="ano">Ano</param>
        /// <param name="mes">Mês (1-12)</param>
        /// <returns>Lista de agendamentos do mês</returns>
        Task<IEnumerable<Agendamento>> BuscarAgendamentosPorMesAsync(int ano, int mes);

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
        /// Busca agendamentos de um cliente
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <returns>Lista de agendamentos do cliente</returns>
        Task<IEnumerable<Agendamento>> BuscarAgendamentosPorClienteAsync(string clienteId);

        /// <summary>
        /// Busca agendamentos por período
        /// </summary>
        /// <param name="dataInicio">Data de início</param>
        /// <param name="dataFim">Data de fim</param>
        /// <returns>Lista de agendamentos no período</returns>
        Task<IEnumerable<Agendamento>> BuscarAgendamentosPorPeriodoAsync(DateTime dataInicio, DateTime dataFim);

        /// <summary>
        /// Busca agendamentos por status
        /// </summary>
        /// <param name="status">Status do agendamento</param>
        /// <returns>Lista de agendamentos com o status</returns>
        Task<IEnumerable<Agendamento>> BuscarAgendamentosPorStatusAsync(StatusAgendamento status);

        /// <summary>
        /// Verifica se existe conflito de horário para um funcionário
        /// </summary>
        /// <param name="funcionarioId">ID do funcionário</param>
        /// <param name="dataHoraInicio">Data/hora de início</param>
        /// <param name="dataHoraFim">Data/hora de fim</param>
        /// <param name="agendamentoIdExcluir">ID do agendamento a excluir da verificação (para edição)</param>
        /// <returns>True se houver conflito</returns>
        Task<bool> VerificarConflitoHorarioAsync(string funcionarioId, DateTime dataHoraInicio, DateTime dataHoraFim, string? agendamentoIdExcluir = null);
    }
}
