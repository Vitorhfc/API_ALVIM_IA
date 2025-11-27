using Client.Controllers.Base;
using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.Entidades.Client;

namespace Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConfiguracaoIAController : ControllerBaseClient<ConfiguracaoIAController>
    {
        #region Campos

        private readonly IConfiguracaoIAService _configuracaoIAService;
        private readonly ILogClientService _logClientService;
        private readonly ILogger<ConfiguracaoIAController> _logger;

        #endregion

        #region Construtor

        public ConfiguracaoIAController(
            IConfiguracaoIAService configuracaoIAService,
            ILogClientService logClientService,
            ILogger<ConfiguracaoIAController> logger)
            : base(logClientService, logger)
        {
            _configuracaoIAService = configuracaoIAService;
            _logClientService = logClientService;
            _logger = logger;
        }

        #endregion

        #region Endpoints - Consultas

        [HttpGet]
        public async Task<IActionResult> ObterConfiguracao()
        {
            try
            {
                var configuracao = await _configuracaoIAService.BuscarConfiguracaoAsync();

                if (configuracao == null)
                {
                    await LogInfoAsync("Nenhuma configuração de IA encontrada", nameof(ObterConfiguracao));
                    return NaoEncontrado("Configuração de IA não encontrada");
                }

                await LogInfoAsync("Configuração de IA obtida", nameof(ObterConfiguracao));

                return Sucesso(configuracao, "Configuração obtida com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ObterConfiguracao));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterConfiguracaoPorId(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID da configuração é obrigatório");

                var configuracao = await _configuracaoIAService.BuscarPorIdAsync(id);

                if (configuracao == null)
                {
                    await LogInfoAsync($"Configuração {id} não encontrada", nameof(ObterConfiguracaoPorId));
                    return NaoEncontrado("Configuração não encontrada");
                }

                return Sucesso(configuracao, "Configuração encontrada");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ObterConfiguracaoPorId), $"ID: {id}");
            }
        }

        #endregion

        #region Endpoints - Operações

        [HttpPost]
        public async Task<IActionResult> CriarConfiguracao([FromBody] ConfiguracaoIA configuracao)
        {
            try
            {
                if (configuracao == null)
                    return Erro("Dados da configuração são obrigatórios");

                var configuracaoExistente = await _configuracaoIAService.BuscarConfiguracaoAsync();
                if (configuracaoExistente != null)
                    return Erro("Já existe uma configuração de IA. Use o endpoint de atualização.");

                configuracao.FlgAtivo = true;
                configuracao.DtaCadastro = DateTime.UtcNow;
                configuracao.DtaAlteracao = DateTime.UtcNow;

                var configuracaoCriada = await _configuracaoIAService.AdicionarAsync(configuracao);

                await RegistraAcaoAsync(
                    "Criar Configuração IA",
                    "Nova configuração",
                    SerializarParaLog(configuracaoCriada),
                    "Configuração de IA criada");

                return Criado($"/api/configuracaoia/{configuracaoCriada.Id}", configuracaoCriada, "Configuração criada com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(CriarConfiguracao), SerializarParaLog(configuracao));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarConfiguracao(string id, [FromBody] ConfiguracaoIA configuracao)
        {
            try
            {
                if (configuracao == null)
                    return Erro("Dados da configuração são obrigatórios");

                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID da configuração é obrigatório");

                var configuracaoAntiga = await _configuracaoIAService.BuscarPorIdAsync(id);
                if (configuracaoAntiga == null)
                    return NaoEncontrado("Configuração não encontrada");

                configuracao.Id = id;
                configuracao.DtaAlteracao = DateTime.UtcNow;

                var configuracaoAtualizada = await _configuracaoIAService.EditarAsync(configuracao);

                await RegistraAcaoAsync(
                    "Atualizar Configuração IA",
                    SerializarParaLog(configuracaoAntiga),
                    SerializarParaLog(configuracaoAtualizada),
                    "Configuração de IA atualizada");

                return Sucesso(configuracaoAtualizada, "Configuração atualizada com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(AtualizarConfiguracao), $"ID: {id}, Dados: {SerializarParaLog(configuracao)}");
            }
        }

        [HttpPatch("{id}/ativar")]
        public async Task<IActionResult> AtivarConfiguracao(string id)
        {
            try
            {
                var configuracao = await _configuracaoIAService.BuscarPorIdAsync(id);
                if (configuracao == null)
                    return NaoEncontrado("Configuração não encontrada");

                configuracao.FlgAtivo = true;
                configuracao.DtaAlteracao = DateTime.UtcNow;

                await _configuracaoIAService.EditarAsync(configuracao);

                await RegistraAcaoAsync(
                    "Ativar Configuração IA",
                    "Configuração inativa",
                    "Configuração ativa",
                    "Configuração de IA ativada");

                return Sucesso(configuracao, "Configuração ativada com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(AtivarConfiguracao), $"ID: {id}");
            }
        }

        [HttpPatch("{id}/inativar")]
        public async Task<IActionResult> InativarConfiguracao(string id)
        {
            try
            {
                var configuracao = await _configuracaoIAService.BuscarPorIdAsync(id);
                if (configuracao == null)
                    return NaoEncontrado("Configuração não encontrada");

                configuracao.FlgAtivo = false;
                configuracao.DtaAlteracao = DateTime.UtcNow;

                await _configuracaoIAService.EditarAsync(configuracao);

                await RegistraAcaoAsync(
                    "Inativar Configuração IA",
                    "Configuração ativa",
                    "Configuração inativa",
                    "Configuração de IA inativada");

                return Sucesso(configuracao, "Configuração inativada com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(InativarConfiguracao), $"ID: {id}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoverConfiguracao(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Erro("ID da configuração é obrigatório");

                var configuracao = await _configuracaoIAService.BuscarPorIdAsync(id);
                if (configuracao == null)
                    return NaoEncontrado("Configuração não encontrada");

                await _configuracaoIAService.ExcluirAsync(configuracao);

                await RegistraAcaoAsync(
                    "Remover Configuração IA",
                    SerializarParaLog(configuracao),
                    "Configuração removida",
                    "Configuração de IA removida");

                return Sucesso(null, "Configuração removida com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(RemoverConfiguracao), $"ID: {id}");
            }
        }

        #endregion
    }
}