using Client.Controllers.Base;
using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.Entidades.Client;

namespace Cliente_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AgendamentoController : ControllerBaseClient<AgendamentoController>
    {
        #region Campos

        private readonly IAgendamentoService _agendamentoService;

        #endregion

        #region Construtor

        public AgendamentoController(
            IAgendamentoService agendamentoService,
            ILogClientService logClientService,
            ILogger<AgendamentoController> logger)
            : base(logClientService, logger)
        {
            _agendamentoService = agendamentoService;
        }

        #endregion

        #region Endpoints - Consultas

        /// <summary>
        /// Busca agendamentos de um mês específico
        /// </summary>
        /// <param name="data">Data de referência (qualquer dia do mês)</param>
        /// <returns>Lista de agendamentos do mês</returns>
        [HttpGet("mes")]
        public async Task<IActionResult> BuscarAgendamentosMes([FromQuery] DateTime data)
        {
            try
            {
                var agendamentos = await _agendamentoService.BuscarAgendamentosPorMesAsync(data);

                await LogInfoAsync($"Buscou {agendamentos.Count()} agendamentos do mês {data.Month}/{data.Year}", nameof(BuscarAgendamentosMes));

                return Sucesso(agendamentos, $"Agendamentos de {data:MMMM/yyyy} listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(BuscarAgendamentosMes), $"Data: {data}");
            }
        }

        /// <summary>
        /// Busca agendamentos de um dia específico
        /// </summary>
        /// <param name="data">Data do dia</param>
        /// <returns>Lista de agendamentos do dia</returns>
        [HttpGet("dia")]
        public async Task<IActionResult> BuscarAgendamentosDia([FromQuery] DateTime data)
        {
            try
            {
                var agendamentos = await _agendamentoService.BuscarAgendamentosPorDiaAsync(data);

                await LogInfoAsync($"Buscou {agendamentos.Count()} agendamentos do dia {data:dd/MM/yyyy}", nameof(BuscarAgendamentosDia));

                return Sucesso(agendamentos, $"Agendamentos de {data:dd/MM/yyyy} listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(BuscarAgendamentosDia), $"Data: {data}");
            }
        }

        /// <summary>
        /// Busca agendamentos de um funcionário em um período
        /// </summary>
        /// <param name="funcionarioId">ID do funcionário</param>
        /// <param name="dataInicio">Data de início do período</param>
        /// <param name="dataFim">Data de fim do período</param>
        /// <returns>Lista de agendamentos do funcionário no período</returns>
        [HttpGet("funcionario/{funcionarioId}")]
        public async Task<IActionResult> BuscarAgendamentosFuncionario(
            string funcionarioId,
            [FromQuery] DateTime dataInicio,
            [FromQuery] DateTime dataFim)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(funcionarioId))
                    return Erro("ID do funcionário é obrigatório");

                var agendamentos = await _agendamentoService.BuscarAgendamentosPorFuncionarioAsync(funcionarioId, dataInicio, dataFim);

                await LogInfoAsync($"Buscou {agendamentos.Count()} agendamentos do funcionário {funcionarioId}", nameof(BuscarAgendamentosFuncionario));

                return Sucesso(agendamentos, "Agendamentos do funcionário listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(BuscarAgendamentosFuncionario), $"FuncionarioId: {funcionarioId}, DataInicio: {dataInicio}, DataFim: {dataFim}");
            }
        }

        /// <summary>
        /// Busca agendamento por ID
        /// </summary>
        /// <param name="id">ID do agendamento</param>
        /// <returns>Agendamento encontrado</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarAgendamentoPorId(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do agendamento é obrigatório");

                var agendamento = await _agendamentoService.BuscarPorIdAsync(id);

                if (agendamento == null)
                {
                    await LogInfoAsync($"Tentativa de buscar agendamento inexistente: {id}", nameof(BuscarAgendamentoPorId));
                    return NaoEncontrado("Agendamento não encontrado");
                }

                await LogInfoAsync($"Buscou agendamento: {agendamento.Titulo}", nameof(BuscarAgendamentoPorId));

                return Sucesso(agendamento, "Agendamento encontrado");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(BuscarAgendamentoPorId), $"ID: {id}");
            }
        }

        #endregion

        #region Endpoints - Operações

        /// <summary>
        /// Cadastra ou edita um agendamento
        /// </summary>
        /// <param name="agendamento">Dados do agendamento</param>
        /// <returns>Agendamento criado/editado</returns>
        [HttpPost]
        public async Task<IActionResult> CadastrarOuEditarAgendamento([FromBody] Agendamento agendamento)
        {
            try
            {
                if (agendamento == null)
                    return Erro("Dados do agendamento são obrigatórios");

                var agendamentoSalvo = await _agendamentoService.CadastrarOuEditarAgendamentoAsync(agendamento);

                var isEdicao = !string.IsNullOrWhiteSpace(agendamento.Id);
                var acao = isEdicao ? "Editar Agendamento" : "Criar Agendamento";

                await RegistraAcaoAsync(
                    acao,
                    isEdicao ? SerializarParaLog(agendamento) : "Novo agendamento",
                    SerializarParaLog(agendamentoSalvo),
                    $"Agendamento {agendamentoSalvo.Titulo} {(isEdicao ? "editado" : "criado")} com sucesso");

                var mensagem = isEdicao ? "Agendamento editado com sucesso" : "Agendamento criado com sucesso";

                return isEdicao
                    ? Sucesso(agendamentoSalvo, mensagem)
                    : Criado($"/api/agendamento/{agendamentoSalvo.Id}", agendamentoSalvo, mensagem);
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(CadastrarOuEditarAgendamento), SerializarParaLog(agendamento));
            }
        }

        /// <summary>
        /// Cancela um agendamento
        /// </summary>
        /// <param name="id">ID do agendamento</param>
        /// <param name="motivo">Motivo do cancelamento</param>
        /// <returns>Agendamento cancelado</returns>
        [HttpPost("{id}/cancelar")]
        public async Task<IActionResult> CancelarAgendamento(string id, [FromBody] CancelarAgendamentoRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do agendamento é obrigatório");

                var agendamento = await _agendamentoService.CancelarAgendamentoAsync(id, request.Motivo);

                await RegistraAcaoAsync(
                    "Cancelar Agendamento",
                    $"Agendamento: {agendamento.Titulo}",
                    $"Motivo: {request.Motivo}",
                    $"Agendamento {agendamento.Titulo} cancelado");

                return Sucesso(agendamento, "Agendamento cancelado com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(CancelarAgendamento), $"ID: {id}");
            }
        }

        /// <summary>
        /// Confirma um agendamento
        /// </summary>
        /// <param name="id">ID do agendamento</param>
        /// <returns>Agendamento confirmado</returns>
        [HttpPost("{id}/confirmar")]
        public async Task<IActionResult> ConfirmarAgendamento(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do agendamento é obrigatório");

                var agendamento = await _agendamentoService.ConfirmarAgendamentoAsync(id);

                await RegistraAcaoAsync(
                    "Confirmar Agendamento",
                    $"Agendamento: {agendamento.Titulo}",
                    "Status: Confirmado",
                    $"Agendamento {agendamento.Titulo} confirmado");

                return Sucesso(agendamento, "Agendamento confirmado com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ConfirmarAgendamento), $"ID: {id}");
            }
        }

        /// <summary>
        /// Inicia atendimento de um agendamento
        /// </summary>
        /// <param name="id">ID do agendamento</param>
        /// <returns>Agendamento em atendimento</returns>
        [HttpPost("{id}/iniciar-atendimento")]
        public async Task<IActionResult> IniciarAtendimento(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do agendamento é obrigatório");

                var agendamento = await _agendamentoService.IniciarAtendimentoAsync(id);

                await RegistraAcaoAsync(
                    "Iniciar Atendimento",
                    $"Agendamento: {agendamento.Titulo}",
                    "Status: Em Atendimento",
                    $"Atendimento iniciado para agendamento {agendamento.Titulo}");

                return Sucesso(agendamento, "Atendimento iniciado com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(IniciarAtendimento), $"ID: {id}");
            }
        }

        /// <summary>
        /// Conclui um agendamento
        /// </summary>
        /// <param name="id">ID do agendamento</param>
        /// <param name="request">Dados de conclusão</param>
        /// <returns>Agendamento concluído</returns>
        [HttpPost("{id}/concluir")]
        public async Task<IActionResult> ConcluirAgendamento(string id, [FromBody] ConcluirAgendamentoRequest? request = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do agendamento é obrigatório");

                var agendamento = await _agendamentoService.ConcluirAgendamentoAsync(id, request?.Observacoes);

                await RegistraAcaoAsync(
                    "Concluir Agendamento",
                    $"Agendamento: {agendamento.Titulo}",
                    "Status: Concluído",
                    $"Agendamento {agendamento.Titulo} concluído");

                return Sucesso(agendamento, "Agendamento concluído com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ConcluirAgendamento), $"ID: {id}");
            }
        }

        #endregion
    }

    #region DTOs

    public class CancelarAgendamentoRequest
    {
        public string Motivo { get; set; } = "";
    }

    public class ConcluirAgendamentoRequest
    {
        public string? Observacoes { get; set; }
    }

    #endregion
}
