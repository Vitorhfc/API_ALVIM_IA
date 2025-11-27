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
    public class FuncionarioController : ControllerBaseClient<FuncionarioController>
    {
        #region Campos

        private readonly IFuncionarioService _funcionarioService;

        #endregion

        #region Construtor

        public FuncionarioController(
            IFuncionarioService funcionarioService,
            ILogClientService logClientService,
            ILogger<FuncionarioController> logger)
            : base(logClientService, logger)
        {
            _funcionarioService = funcionarioService;
        }

        #endregion

        #region Endpoints

        /// <summary>
        /// Adiciona ou edita um funcionário
        /// </summary>
        /// <param name="request">Dados do funcionário e senha do usuário</param>
        /// <returns>Funcionário criado/editado</returns>
        [HttpPost]
        public async Task<IActionResult> AdicionarOuEditarFuncionario([FromBody] AdicionarEditarFuncionarioRequest request)
        {
            try
            {
                if (request == null)
                    return Erro("Dados do funcionário são obrigatórios");

                if (request.Funcionario == null)
                    return Erro("Funcionário é obrigatório");

                // Obter empresa do contexto do usuário logado
                var empresaId = ObterIdEmpresaObrigatorio();

                var funcionario = await _funcionarioService.AdicionarOuEditarFuncionarioComUsuarioAsync(
                    request.Funcionario,
                    request.Nome,
                    request.Email,
                    request.Cpf,
                    request.Celular,
                    request.Senha,
                    empresaId);

                var isEdicao = !string.IsNullOrWhiteSpace(request.Funcionario.Id);
                var acao = isEdicao ? "Editar Funcionário" : "Criar Funcionário";

                await RegistraAcaoAsync(
                    acao,
                    isEdicao ? SerializarParaLog(request.Funcionario) : "Novo funcionário",
                    SerializarParaLog(funcionario),
                    $"Funcionário {request.Nome} {(isEdicao ? "editado" : "criado")} com sucesso");

                var mensagem = isEdicao ? "Funcionário editado com sucesso" : "Funcionário criado com sucesso";

                return isEdicao
                    ? Sucesso(funcionario, mensagem)
                    : Criado($"/api/funcionario/{funcionario.Id}", funcionario, mensagem);
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(AdicionarOuEditarFuncionario), SerializarParaLog(request));
            }
        }

        /// <summary>
        /// Lista todos os funcionários
        /// </summary>
        /// <returns>Lista de funcionários</returns>
        [HttpGet]
        public async Task<IActionResult> ListarFuncionarios()
        {
            try
            {
                var funcionarios = await _funcionarioService.BuscarTodosAsync();

                await LogInfoAsync($"Listou {funcionarios.Count()} funcionários", nameof(ListarFuncionarios));

                return Sucesso(funcionarios, "Funcionários listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarFuncionarios));
            }
        }

        /// <summary>
        /// Recupera funcionário por id
        /// </summary>
        /// <param name="id">ID do funcionário</param>
        /// <returns>Funcionário encontrado</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarFuncionarioPorId(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do funcionário é obrigatório");

                var funcionario = await _funcionarioService.BuscarPorIdAsync(id);

                if (funcionario == null)
                {
                    await LogInfoAsync($"Tentativa de buscar funcionário inexistente: {id}", nameof(BuscarFuncionarioPorId));
                    return NaoEncontrado("Funcionário não encontrado");
                }

                await LogInfoAsync($"Buscou funcionário ID: {id}", nameof(BuscarFuncionarioPorId));

                return Sucesso(funcionario, "Funcionário encontrado");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(BuscarFuncionarioPorId), $"ID: {id}");
            }
        }

        /// <summary>
        /// Atualiza horários de disponibilidade de atendimento do funcionário
        /// </summary>
        /// <param name="id">ID do funcionário</param>
        /// <param name="horarios">Lista de horários de atendimento</param>
        /// <returns>Confirmação da atualização</returns>
        [HttpPost("{id}/horarios")]
        public async Task<IActionResult> AtualizarHorariosAtendimento(string id, [FromBody] List<HorarioAtendimento> horarios)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do funcionário é obrigatório");

                await _funcionarioService.AtualizarHorariosAtendimentoAsync(id, horarios);

                var funcionario = await _funcionarioService.BuscarPorIdAsync(id);

                await RegistraAcaoAsync(
                    "Atualizar Horários de Atendimento",
                    $"Funcionário ID: {id}",
                    SerializarParaLog(horarios),
                    $"Horários de atendimento atualizados para funcionário ID: {id}");

                return Sucesso(horarios, "Horários de atendimento atualizados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(AtualizarHorariosAtendimento), $"ID: {id}");
            }
        }

        /// <summary>
        /// Recupera os horários de atendimento do funcionário
        /// </summary>
        /// <param name="id">ID do funcionário</param>
        /// <returns>Lista de horários de atendimento</returns>
        [HttpGet("{id}/horarios")]
        public async Task<IActionResult> RecuperarHorariosAtendimento(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do funcionário é obrigatório");

                var horarios = await _funcionarioService.RecuperarHorariosAtendimentoAsync(id);

                var funcionario = await _funcionarioService.BuscarPorIdAsync(id);

                await LogInfoAsync($"Recuperou horários de atendimento do funcionário ID: {id}", nameof(RecuperarHorariosAtendimento));

                return Sucesso(horarios, "Horários de atendimento recuperados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(RecuperarHorariosAtendimento), $"ID: {id}");
            }
        }

        /// <summary>
        /// Inativa ou ativa funcionário
        /// </summary>
        /// <param name="id">ID do funcionário</param>
        /// <param name="ativo">Status (true = ativo, false = inativo)</param>
        /// <returns>Funcionário com status atualizado</returns>
        [HttpPost("{id}/status")]
        public async Task<IActionResult> AlterarStatusFuncionario(string id, [FromQuery] bool ativo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do funcionário é obrigatório");

                var funcionario = await _funcionarioService.AlterarStatusFuncionarioAsync(id, ativo);

                var acao = ativo ? "Ativar Funcionário" : "Inativar Funcionário";
                var mensagem = ativo ? "ativado" : "inativado";

                await RegistraAcaoAsync(
                    acao,
                    $"Funcionário ID {id} {(!ativo ? "ativo" : "inativo")}",
                    $"Funcionário ID {id} {(ativo ? "ativo" : "inativo")}",
                    $"Funcionário ID {id} foi {mensagem}");

                return Sucesso(funcionario, $"Funcionário {mensagem} com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(AlterarStatusFuncionario), $"ID: {id}, Ativo: {ativo}");
            }
        }

        /// <summary>
        /// Lista funcionários ativos
        /// </summary>
        /// <returns>Lista de funcionários ativos</returns>
        [HttpGet("ativos")]
        public async Task<IActionResult> ListarFuncionariosAtivos()
        {
            try
            {
                var funcionarios = await _funcionarioService.BuscarFuncionariosAtivosAsync();

                await LogInfoAsync($"Listou {funcionarios.Count()} funcionários ativos", nameof(ListarFuncionariosAtivos));

                return Sucesso(funcionarios, "Funcionários ativos listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarFuncionariosAtivos));
            }
        }

        #endregion
    }

    #region DTOs

    public class AdicionarEditarFuncionarioRequest
    {
        public Funcionario Funcionario { get; set; } = new Funcionario();
        public string Nome { get; set; } = "";
        public string Email { get; set; } = "";
        public string Cpf { get; set; } = "";
        public string Celular { get; set; } = "";
        public string Senha { get; set; } = "";
    }

    #endregion
}
