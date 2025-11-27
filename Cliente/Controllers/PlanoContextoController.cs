using Client.Controllers.Base;
using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.Model;

namespace Client.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de Planos de Contexto da IA
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlanoContextoController : ControllerBaseClient<PlanoContextoController>
    {
        #region Campos

        private readonly IPlanoContextoService _planoContextoService;
        private readonly ILogClientService _logClientService;
        private readonly ILogger<PlanoContextoController> _logger;

        #endregion

        #region Construtor

        public PlanoContextoController(
            IPlanoContextoService planoContextoService,
            ILogClientService logClientService,
            ILogger<PlanoContextoController> logger)
            : base(logClientService, logger)
        {
            _planoContextoService = planoContextoService;
            _logClientService = logClientService;
            _logger = logger;
        }

        #endregion

        #region Endpoints - Consultas

        /// <summary>
        /// Lista todos os planos de contexto
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ListarPlanos()
        {
            try
            {
                var planos = await _planoContextoService.BuscarTodosAsync();

                await LogInfoAsync($"Listou {planos.Count} planos de contexto", nameof(ListarPlanos));

                return Sucesso(planos, "Planos listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarPlanos));
            }
        }

        /// <summary>
        /// Lista apenas planos ativos
        /// </summary>
        [HttpGet("ativos")]
        public async Task<IActionResult> ListarPlanosAtivos()
        {
            try
            {
                var planos = await _planoContextoService.BuscarAtivosAsync();

                await LogInfoAsync($"Listou {planos.Count} planos ativos", nameof(ListarPlanosAtivos));

                return Sucesso(planos, "Planos ativos listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarPlanosAtivos));
            }
        }

        /// <summary>
        /// Busca plano por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarPlanoPorId(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do plano é obrigatório");

                var plano = await _planoContextoService.BuscarPorIdAsync(id);

                if (plano == null)
                {
                    await LogInfoAsync($"Plano {id} não encontrado", nameof(BuscarPlanoPorId));
                    return NaoEncontrado("Plano não encontrado");
                }

                return Sucesso(plano, "Plano encontrado");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(BuscarPlanoPorId), $"ID: {id}");
            }
        }

        #endregion

        #region Endpoints - Operações

        /// <summary>
        /// Cria um novo plano de contexto
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CriarPlano([FromBody] CriarPlanoContextoRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Erro("Dados inválidos. Verifique os campos obrigatórios.");

                var plano = await _planoContextoService.CriarAsync(request);

                await LogInfoAsync(
                    $"Plano criado: {plano.Nome} (Tipo: {plano.Tipo})",
                    nameof(CriarPlano)
                );

                return Sucesso(plano, "Plano criado com sucesso");
            }
            catch (InvalidOperationException ex)
            {
                return Erro(ex.Message);
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(CriarPlano), SerializarParaLog(request));
            }
        }

        /// <summary>
        /// Atualiza um plano existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarPlano(string id, [FromBody] AtualizarPlanoContextoRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do plano é obrigatório");

                if (!ModelState.IsValid)
                    return Erro("Dados inválidos");

                var plano = await _planoContextoService.AtualizarAsync(id, request);

                await LogInfoAsync(
                    $"Plano atualizado: {plano.Nome}",
                    nameof(AtualizarPlano)
                );

                return Sucesso(plano, "Plano atualizado com sucesso");
            }
            catch (KeyNotFoundException ex)
            {
                return NaoEncontrado(ex.Message);
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(AtualizarPlano), $"ID: {id}, Dados: {SerializarParaLog(request)}");
            }
        }

        /// <summary>
        /// Deleta um plano (apenas se não for padrão)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletarPlano(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID do plano é obrigatório");

                var deletado = await _planoContextoService.DeletarAsync(id);

                if (!deletado)
                    return Erro("Não foi possível deletar o plano");

                await LogInfoAsync($"Plano deletado: {id}", nameof(DeletarPlano));

                return Sucesso(null, "Plano deletado com sucesso");
            }
            catch (KeyNotFoundException ex)
            {
                return NaoEncontrado(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Erro(ex.Message);
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(DeletarPlano), $"ID: {id}");
            }
        }

        #endregion

        #region Endpoints - Configuração de Planos

        /// <summary>
        /// Configura múltiplos planos (ativa/desativa)
        /// IMPORTANTE: Plano Base sempre ficará ativo
        /// </summary>
        [HttpPost("configurar")]
        public async Task<IActionResult> ConfigurarPlanos([FromBody] ConfigurarPlanosRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Erro("Dados inválidos. Verifique a lista de planos.");

                await LogInfoAsync(
                    $"Iniciando configuração de {request.Planos.Count} planos",
                    nameof(ConfigurarPlanos)
                );

                var resultado = await _planoContextoService.ConfigurarPlanosAsync(request);

                await LogInfoAsync(
                    $"Configuração concluída: {resultado.PlanosAtualizados}/{request.Planos.Count} planos atualizados",
                    nameof(ConfigurarPlanos)
                );

                if (resultado.Sucesso)
                {
                    return Sucesso(resultado, resultado.Mensagem);
                }
                else
                {
                    return BadRequest(new
                    {
                        sucesso = false,
                        mensagem = resultado.Mensagem,
                        dados = resultado,
                        timestamp = DateTimeOffset.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ConfigurarPlanos), SerializarParaLog(request));
            }
        }

        /// <summary>
        /// Aplica planos ativos na ConfiguracaoIA
        /// Preenche os campos da ConfiguracaoIA com base nos planos ativos
        /// </summary>
        [HttpPost("aplicar/{configuracaoIAId}")]
        public async Task<IActionResult> AplicarPlanosNaConfiguracao(string configuracaoIAId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configuracaoIAId))
                    return Erro("ID da ConfiguracaoIA é obrigatório");

                await LogInfoAsync(
                    $"Aplicando planos na ConfiguracaoIA: {configuracaoIAId}",
                    nameof(AplicarPlanosNaConfiguracao)
                );

                var sucesso = await _planoContextoService.AplicarPlanosNaConfiguracaoAsync(configuracaoIAId);

                if (!sucesso)
                {
                    await LogInfoAsync(
                        $"ConfiguracaoIA {configuracaoIAId} não encontrada",
                        nameof(AplicarPlanosNaConfiguracao)
                    );
                    return NaoEncontrado("ConfiguracaoIA não encontrada");
                }

                await LogInfoAsync(
                    $"Planos aplicados com sucesso na ConfiguracaoIA: {configuracaoIAId}",
                    nameof(AplicarPlanosNaConfiguracao)
                );

                return Sucesso(null, "Planos aplicados com sucesso na configuração da IA");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(AplicarPlanosNaConfiguracao), $"ConfiguracaoIAId: {configuracaoIAId}");
            }
        }

        /// <summary>
        /// Inicializa planos padrão do sistema (se não existirem)
        /// </summary>
        [HttpPost("inicializar-padroes")]
        public async Task<IActionResult> InicializarPlanosPadrao()
        {
            try
            {
                await LogInfoAsync("Inicializando planos padrão", nameof(InicializarPlanosPadrao));

                await _planoContextoService.InicializarPlanosPadraoAsync();

                await LogInfoAsync("Planos padrão inicializados", nameof(InicializarPlanosPadrao));

                return Sucesso(null, "Planos padrão inicializados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(InicializarPlanosPadrao));
            }
        }

        #endregion
    }
}
