using Client_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Client_Service.ServiceGenerico;
using Shared.Classes.Entidades.Client;

namespace Client_Service.Service
{
    public class AgendamentoService : ServiceGenerico<Agendamento>, IAgendamentoService
    {
        private readonly IAgendamentoRepositorio _agendamentoRepositorio;
        private readonly IClienteRepositorio _clienteRepositorio;
        private readonly IFuncionarioRepositorio _funcionarioRepositorio;

        public AgendamentoService(
            IAgendamentoRepositorio agendamentoRepositorio,
            IClienteRepositorio clienteRepositorio,
            IFuncionarioRepositorio funcionarioRepositorio)
            : base(agendamentoRepositorio)
        {
            _agendamentoRepositorio = agendamentoRepositorio;
            _clienteRepositorio = clienteRepositorio;
            _funcionarioRepositorio = funcionarioRepositorio;
        }

        public async Task<IEnumerable<Agendamento>> BuscarAgendamentosPorMesAsync(DateTime data)
        {
            try
            {
                return await _agendamentoRepositorio.BuscarAgendamentosPorMesAsync(data.Year, data.Month);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar agendamentos do mês: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<Agendamento>> BuscarAgendamentosPorDiaAsync(DateTime data)
        {
            try
            {
                return await _agendamentoRepositorio.BuscarAgendamentosPorDiaAsync(data);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar agendamentos do dia: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<Agendamento>> BuscarAgendamentosPorFuncionarioAsync(string funcionarioId, DateTime dataInicio, DateTime dataFim)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(funcionarioId))
                    throw new ArgumentException("ID do funcionário é obrigatório");

                return await _agendamentoRepositorio.BuscarAgendamentosPorFuncionarioAsync(funcionarioId, dataInicio, dataFim);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar agendamentos do funcionário: {ex.Message}", ex);
            }
        }

        public async Task<Agendamento> CadastrarOuEditarAgendamentoAsync(Agendamento agendamento)
        {
            try
            {
                // Validações básicas
                if (string.IsNullOrWhiteSpace(agendamento.ClienteId))
                    throw new ArgumentException("Cliente é obrigatório");

                if (agendamento.FuncionarioIds == null || !agendamento.FuncionarioIds.Any())
                    throw new ArgumentException("Pelo menos um funcionário deve ser selecionado");

                if (agendamento.DataHoraInicio >= agendamento.DataHoraFim)
                    throw new ArgumentException("Data/hora de início deve ser anterior à data/hora de fim");

                if (agendamento.DataHoraInicio < DateTime.Now.AddMinutes(-5))
                    throw new ArgumentException("Não é possível criar agendamento no passado");

                // Validar se cliente existe
                var cliente = await _clienteRepositorio.BuscarPorIdAsync(agendamento.ClienteId);
                if (cliente == null)
                    throw new ArgumentException("Cliente não encontrado");

                // Validar se funcionários existem
                foreach (var funcionarioId in agendamento.FuncionarioIds)
                {
                    var funcionario = await _funcionarioRepositorio.BuscarPorIdAsync(funcionarioId);
                    if (funcionario == null)
                        throw new ArgumentException($"Funcionário {funcionarioId} não encontrado");

                    if (!funcionario.FlgAtivo)
                        throw new ArgumentException($"Funcionário {funcionarioId} está inativo");
                }

                // Verificar conflitos de horário
                var conflitos = await VerificarConflitoHorarioAsync(
                    agendamento.FuncionarioIds,
                    agendamento.DataHoraInicio,
                    agendamento.DataHoraFim,
                    agendamento.Id);

                if (conflitos.Any())
                {
                    throw new InvalidOperationException($"Conflito de horário para os funcionários IDs: {string.Join(", ", conflitos)}");
                }

                bool isEdicao = !string.IsNullOrWhiteSpace(agendamento.Id);

                if (isEdicao)
                {
                    var agendamentoExistente = await _agendamentoRepositorio.BuscarPorIdAsync(agendamento.Id);
                    if (agendamentoExistente == null)
                        throw new KeyNotFoundException("Agendamento não encontrado");

                    agendamento.DtaAlteracao = DateTime.UtcNow;
                    agendamento.DtaCadastro = agendamentoExistente.DtaCadastro;

                    return await _agendamentoRepositorio.EditarAsync(agendamento);
                }
                else
                {
                    agendamento.Status = StatusAgendamento.Agendado;
                    agendamento.FlgAtivo = true;
                    agendamento.DtaCadastro = DateTime.UtcNow;
                    agendamento.DtaAlteracao = DateTime.UtcNow;

                    return await _agendamentoRepositorio.AdicionarAsync(agendamento);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao cadastrar/editar agendamento: {ex.Message}", ex);
            }
        }

        public async Task<Agendamento> CancelarAgendamentoAsync(string agendamentoId, string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(agendamentoId))
                    throw new ArgumentException("ID do agendamento é obrigatório");

                var agendamento = await _agendamentoRepositorio.BuscarPorIdAsync(agendamentoId);
                if (agendamento == null)
                    throw new KeyNotFoundException("Agendamento não encontrado");

                if (agendamento.Status == StatusAgendamento.Cancelado)
                    throw new InvalidOperationException("Agendamento já está cancelado");

                if (agendamento.Status == StatusAgendamento.Concluido)
                    throw new InvalidOperationException("Não é possível cancelar um agendamento concluído");

                agendamento.Status = StatusAgendamento.Cancelado;
                agendamento.DtaCancelamento = DateTime.UtcNow;
                agendamento.MotivoCancelamento = motivo;
                agendamento.DtaAlteracao = DateTime.UtcNow;

                return await _agendamentoRepositorio.EditarAsync(agendamento);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao cancelar agendamento: {ex.Message}", ex);
            }
        }

        public async Task<Agendamento> ConfirmarAgendamentoAsync(string agendamentoId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(agendamentoId))
                    throw new ArgumentException("ID do agendamento é obrigatório");

                var agendamento = await _agendamentoRepositorio.BuscarPorIdAsync(agendamentoId);
                if (agendamento == null)
                    throw new KeyNotFoundException("Agendamento não encontrado");

                if (agendamento.Status != StatusAgendamento.Agendado)
                    throw new InvalidOperationException("Apenas agendamentos com status 'Agendado' podem ser confirmados");

                agendamento.Status = StatusAgendamento.Confirmado;
                agendamento.DtaAlteracao = DateTime.UtcNow;

                return await _agendamentoRepositorio.EditarAsync(agendamento);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao confirmar agendamento: {ex.Message}", ex);
            }
        }

        public async Task<Agendamento> IniciarAtendimentoAsync(string agendamentoId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(agendamentoId))
                    throw new ArgumentException("ID do agendamento é obrigatório");

                var agendamento = await _agendamentoRepositorio.BuscarPorIdAsync(agendamentoId);
                if (agendamento == null)
                    throw new KeyNotFoundException("Agendamento não encontrado");

                if (agendamento.Status == StatusAgendamento.Cancelado)
                    throw new InvalidOperationException("Não é possível iniciar atendimento de um agendamento cancelado");

                if (agendamento.Status == StatusAgendamento.Concluido)
                    throw new InvalidOperationException("Agendamento já está concluído");

                agendamento.Status = StatusAgendamento.EmAtendimento;
                agendamento.DtaAlteracao = DateTime.UtcNow;

                return await _agendamentoRepositorio.EditarAsync(agendamento);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao iniciar atendimento: {ex.Message}", ex);
            }
        }

        public async Task<Agendamento> ConcluirAgendamentoAsync(string agendamentoId, string? observacoes = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(agendamentoId))
                    throw new ArgumentException("ID do agendamento é obrigatório");

                var agendamento = await _agendamentoRepositorio.BuscarPorIdAsync(agendamentoId);
                if (agendamento == null)
                    throw new KeyNotFoundException("Agendamento não encontrado");

                if (agendamento.Status == StatusAgendamento.Cancelado)
                    throw new InvalidOperationException("Não é possível concluir um agendamento cancelado");

                if (agendamento.Status == StatusAgendamento.Concluido)
                    throw new InvalidOperationException("Agendamento já está concluído");

                agendamento.Status = StatusAgendamento.Concluido;
                agendamento.DtaConclusao = DateTime.UtcNow;
                agendamento.DtaAlteracao = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(observacoes))
                {
                    agendamento.Observacoes = string.IsNullOrWhiteSpace(agendamento.Observacoes)
                        ? observacoes
                        : $"{agendamento.Observacoes}\n\n[Conclusão] {observacoes}";
                }

                return await _agendamentoRepositorio.EditarAsync(agendamento);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao concluir agendamento: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> VerificarConflitoHorarioAsync(List<string> funcionarioIds, DateTime dataHoraInicio, DateTime dataHoraFim, string? agendamentoIdExcluir = null)
        {
            try
            {
                var funcionariosComConflito = new List<string>();

                foreach (var funcionarioId in funcionarioIds)
                {
                    var temConflito = await _agendamentoRepositorio.VerificarConflitoHorarioAsync(
                        funcionarioId,
                        dataHoraInicio,
                        dataHoraFim,
                        agendamentoIdExcluir);

                    if (temConflito)
                        funcionariosComConflito.Add(funcionarioId);
                }

                return funcionariosComConflito;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao verificar conflito de horário: {ex.Message}", ex);
            }
        }

        protected override async Task ValidarEntidade(Agendamento entidade)
        {
            var erros = new List<string>();

            if (string.IsNullOrWhiteSpace(entidade.ClienteId))
                erros.Add("Cliente é obrigatório");

            if (entidade.FuncionarioIds == null || !entidade.FuncionarioIds.Any())
                erros.Add("Pelo menos um funcionário deve ser selecionado");

            if (string.IsNullOrWhiteSpace(entidade.Titulo))
                erros.Add("Título é obrigatório");

            if (entidade.DataHoraInicio == default)
                erros.Add("Data/hora de início é obrigatória");

            if (entidade.DataHoraFim == default)
                erros.Add("Data/hora de fim é obrigatória");

            if (entidade.DataHoraInicio >= entidade.DataHoraFim)
                erros.Add("Data/hora de início deve ser anterior à data/hora de fim");

            if (erros.Any())
                throw new ArgumentException($"Validação falhou: {string.Join(", ", erros)}");

            await Task.CompletedTask;
        }
    }
}
