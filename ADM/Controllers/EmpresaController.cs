using ADM.Controllers.Base;
using Admin_Repository.Repositorio.Interface;
using Admin_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.Entidades.ADM;
using Shared.Classes.Model;
using System.Security.Claims;

namespace ADM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmpresaController : ControllerBaseComplemento<EmpresaController>
    {
        #region Campos

        private readonly IEmpresaService _empresaService;
        private readonly IEmpresaProvisionamentoService _provisionamentoService;
        private readonly IUsuarioEmpresaRepository _usuarioEmpresaRepository;

        #endregion

        #region Construtor

        public EmpresaController(
            IEmpresaService empresaService,
            IEmpresaProvisionamentoService provisionamentoService,
            IUsuarioEmpresaRepository usuarioEmpresaRepository,
            ILogADMService logADMService,
            ILogger<EmpresaController> logger)
            : base(logADMService, logger)
        {
            _empresaService = empresaService;
            _provisionamentoService = provisionamentoService;
            _usuarioEmpresaRepository = usuarioEmpresaRepository;
        }

        #endregion

        #region Métodos Privados

        private string ObterUsuarioIdLogado()
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(usuarioId))
                throw new UnauthorizedAccessException("Usuário não autenticado");

            return usuarioId;
        }

        #endregion

        #region Endpoints

        [HttpGet]
        public async Task<IActionResult> ListarEmpresas()
        {
            try
            {
                var empresas = await _empresaService.BuscarTodosAsync();

                await LogInfoAsync($"Listou {empresas.Count()} empresas", nameof(ListarEmpresas));

                return Sucesso(empresas, "Empresas listadas com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ListarEmpresas));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarEmpresaPorId(string id)
        {
            try
            {
                var empresa = await _empresaService.BuscarPorIdAsync(id);

                if (empresa == null)
                {
                    await LogInfoAsync($"Tentativa de buscar empresa inexistente: {id}", nameof(BuscarEmpresaPorId));
                    return Erro("Empresa não encontrada");
                }

                await LogInfoAsync($"Buscou empresa: {empresa.Nome}", nameof(BuscarEmpresaPorId));

                return Sucesso(empresa, "Empresa encontrada");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(BuscarEmpresaPorId), $"ID: {id}");
            }
        }

        [HttpGet("cnpj/{cnpj}")]
        public async Task<IActionResult> BuscarEmpresaPorCnpj(string cnpj)
        {
            try
            {
                var empresa = await _empresaService.BuscarPorCnpjAsync(cnpj);

                if (empresa == null)
                {
                    await LogInfoAsync($"Tentativa de buscar empresa inexistente por CNPJ: {cnpj}", nameof(BuscarEmpresaPorCnpj));
                    return Erro("Empresa não encontrada");
                }

                await LogInfoAsync($"Buscou empresa por CNPJ: {cnpj}", nameof(BuscarEmpresaPorCnpj));

                return Sucesso(empresa, "Empresa encontrada");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(BuscarEmpresaPorCnpj), $"CNPJ: {cnpj}");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CriarEmpresa([FromBody] Empresa empresa)
        {
            try
            {
                if (empresa == null)
                    return Erro("Dados da empresa são obrigatórios");

                var usuarioId = ObterUsuarioIdLogado();
                var dadosAntigos = "Nova empresa";
                var empresaCriada = await _empresaService.CadastrarEmpresaComUsuarioAsync(empresa, usuarioId);

                await RegistraAcaoAsync(
                    "Criar Empresa",
                    dadosAntigos,
                    SerializarParaLog(empresaCriada),
                    $"Empresa {empresa.Nome} criada com sucesso e vinculada ao usuário {usuarioId}");

                if (!string.IsNullOrWhiteSpace(empresaCriada.WahaSessionName))
                {
                    try
                    {
                        await LogInfoAsync($"Iniciando provisionamento WAHA para empresa {empresaCriada.Id}", nameof(CriarEmpresa));

                        var resultadoProvisionamento = await _provisionamentoService.ProvisionarEmpresaAsync(empresaCriada);

                        if (resultadoProvisionamento.Sucesso)
                        {
                            await LogInfoAsync(
                                $"Provisionamento WAHA concluído com sucesso para empresa {empresaCriada.Id}",
                                nameof(CriarEmpresa)
                            );
                        }
                        else
                        {
                            await LogInfoAsync(
                                $"Aviso no provisionamento WAHA: {resultadoProvisionamento.Mensagem}",
                                nameof(CriarEmpresa)
                            );
                        }
                    }
                    catch (Exception exProv)
                    {
                        await LogInfoAsync(
                            $"Erro no provisionamento WAHA (empresa criada, mas sem instância): {exProv.Message}",
                            nameof(CriarEmpresa)
                        );
                    }
                }

                return Sucesso(empresaCriada, "Empresa criada com sucesso");
            }
            catch (UnauthorizedAccessException ex)
            {
                return await LogErroAsync(ex, nameof(CriarEmpresa), "Usuário não autenticado");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(CriarEmpresa), SerializarParaLog(empresa));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarEmpresa(string id, [FromBody] Empresa empresa)
        {
            try
            {
                if (empresa == null)
                    return Erro("Dados da empresa são obrigatórios");

                empresa.Id = id;

                var empresaAntiga = await _empresaService.BuscarPorIdAsync(id);
                if (empresaAntiga == null)
                    return Erro("Empresa não encontrada");

                var empresaAtualizada = await _empresaService.EditarAsync(empresa);

                await RegistraAcaoAsync(
                    "Atualizar Empresa",
                    SerializarParaLog(empresaAntiga),
                    SerializarParaLog(empresaAtualizada),
                    $"Empresa {empresa.Nome} atualizada com sucesso");

                return Sucesso(empresaAtualizada, "Empresa atualizada com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(AtualizarEmpresa), $"ID: {id}, Dados: {SerializarParaLog(empresa)}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoverEmpresa(string id)
        {
            try
            {
                var empresa = await _empresaService.BuscarPorIdAsync(id);
                if (empresa == null)
                    return Erro("Empresa não encontrada");

                await _empresaService.ExcluirPorIdAsync(id);

                await RegistraAcaoAsync(
                    "Remover Empresa",
                    SerializarParaLog(empresa),
                    "Empresa removida",
                    $"Empresa {empresa.Nome} removida com sucesso");

                return Sucesso(null, "Empresa removida com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(RemoverEmpresa), $"ID: {id}");
            }
        }

        /// <summary>
        /// Provisiona uma empresa existente (cria instância WAHA e configura webhook)
        /// </summary>
        [HttpPost("{id}/provisionar")]
        public async Task<IActionResult> ProvisionarEmpresa(string id)
        {
            try
            {
                var empresa = await _empresaService.BuscarPorIdAsync(id);
                if (empresa == null)
                    return Erro("Empresa não encontrada");

                await LogInfoAsync($"Iniciando provisionamento manual para empresa {id}", nameof(ProvisionarEmpresa));

                var resultado = await _provisionamentoService.ProvisionarEmpresaAsync(empresa);

                await RegistraAcaoAsync(
                    "Provisionar Empresa",
                    SerializarParaLog(empresa),
                    SerializarParaLog(resultado),
                    resultado.Sucesso
                        ? $"Empresa {empresa.Nome} provisionada com sucesso"
                        : $"Falha ao provisionar empresa {empresa.Nome}: {resultado.Mensagem}");

                if (resultado.Sucesso)
                    return Sucesso(resultado, resultado.Mensagem);
                else
                    return BadRequest(new
                    {
                        sucesso = false,
                        mensagem = resultado.Mensagem,
                        erro = resultado.Erro,
                        logs = resultado.Logs,
                        timestamp = DateTimeOffset.UtcNow
                    });
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ProvisionarEmpresa), $"ID: {id}");
            }
        }

        /// <summary>
        /// Obtém o QR Code para conectar WhatsApp
        /// </summary>
        [HttpGet("{id}/qrcode")]
        public async Task<IActionResult> ObterQrCode(string id)
        {
            try
            {
                var empresa = await _empresaService.BuscarPorIdAsync(id);
                if (empresa == null)
                    return Erro("Empresa não encontrada");

                if (string.IsNullOrWhiteSpace(empresa.WahaSessionName))
                {
                    return Erro("Empresa não possui SessionName WAHA configurada");
                }

                await LogInfoAsync($"Solicitando QR Code para empresa {id}", nameof(ObterQrCode));

                // Usar o WhatsAppService via provisionamento service
                var status = await _provisionamentoService.VerificarStatusConexaoAsync(id);

                if (status.Conectado)
                {
                    return Sucesso(new { conectado = true, status = status.Status }, "WhatsApp já está conectado");
                }

                // Se não conectado, retornar que precisa obter via outro método
                // (o QR code precisa ser obtido diretamente do WhatsAppService)
                return Sucesso(
                    new
                    {
                        conectado = false,
                        status = status.Status,
                        mensagem = "Use o serviço WhatsApp para obter o QR Code",
                        sessionName = empresa.WahaSessionName
                    },
                    "Sessão não conectada"
                );
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ObterQrCode), $"ID: {id}");
            }
        }

        /// <summary>
        /// Verifica o status de conexão do WhatsApp
        /// </summary>
        [HttpGet("{id}/status-waha")]
        public async Task<IActionResult> ObterStatusWaha(string id)
        {
            try
            {
                var empresa = await _empresaService.BuscarPorIdAsync(id);
                if (empresa == null)
                    return Erro("Empresa não encontrada");

                if (string.IsNullOrWhiteSpace(empresa.WahaSessionName))
                {
                    return Sucesso(
                        new { configurado = false },
                        "Empresa não possui sessão WAHA configurada"
                    );
                }

                var status = await _provisionamentoService.VerificarStatusConexaoAsync(id);

                await LogInfoAsync($"Status WAHA verificado para empresa {id}: {status.Status}", nameof(ObterStatusWaha));

                return Sucesso(
                    new
                    {
                        configurado = true,
                        conectado = status.Conectado,
                        status = status.Status,
                        mensagem = status.Mensagem,
                        ultimaVerificacao = status.UltimaVerificacao,
                        sessionName = empresa.WahaSessionName,
                        numeroWhatsApp = empresa.WahaNumeroWhatsApp,
                        dataConexao = empresa.WahaDataConexao
                    },
                    "Status obtido com sucesso"
                );
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ObterStatusWaha), $"ID: {id}");
            }
        }

        /// <summary>
        /// Reconecta a instância WAHA (reinicia sessão)
        /// </summary>
        [HttpPost("{id}/reconectar")]
        public async Task<IActionResult> ReconectarInstancia(string id)
        {
            try
            {
                var empresa = await _empresaService.BuscarPorIdAsync(id);
                if (empresa == null)
                    return Erro("Empresa não encontrada");

                await LogInfoAsync($"Iniciando reconexão para empresa {id}", nameof(ReconectarInstancia));

                var sucesso = await _provisionamentoService.ReconectarInstanciaAsync(id);

                await RegistraAcaoAsync(
                    "Reconectar Instância",
                    SerializarParaLog(empresa),
                    sucesso ? "Reconexão iniciada" : "Falha na reconexão",
                    sucesso
                        ? $"Reconexão iniciada para empresa {empresa.Nome}"
                        : $"Falha ao iniciar reconexão para empresa {empresa.Nome}");

                if (sucesso)
                    return Sucesso(null, "Reconexão iniciada. Aguarde alguns instantes e escaneie o QR Code novamente");
                else
                    return Erro("Falha ao iniciar reconexão");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ReconectarInstancia), $"ID: {id}");
            }
        }

        /// <summary>
        /// Configura ou reconfigura WAHA para uma empresa.
        /// Requer que o usuário logado seja administrador da empresa.
        /// </summary>
        [HttpPost("{id}/configurar-waha")]
        public async Task<IActionResult> ConfigurarWaha(string id, [FromBody] ConfigurarWahaRequest request)
        {
            try
            {
                // 1. Validar request
                if (!ModelState.IsValid)
                    return Erro("Dados inválidos. Verifique os campos obrigatórios.");

                // 2. Obter usuário logado
                var usuarioId = ObterUsuarioIdLogado();

                await LogInfoAsync(
                    $"Usuário {usuarioId} iniciando configuração WAHA para empresa {id}",
                    nameof(ConfigurarWaha)
                );

                // 3. Verificar se empresa existe
                var empresa = await _empresaService.BuscarPorIdAsync(id);
                if (empresa == null)
                    return Erro("Empresa não encontrada");

                // 4. SEGURANÇA: Verificar se usuário tem vínculo ATIVO e é ADMIN da empresa
                var isAdminDaEmpresa = await _usuarioEmpresaRepository
                    .VerificarSeUsuarioEAdminDaEmpresaAsync(usuarioId, id);

                if (!isAdminDaEmpresa)
                {
                    await LogInfoAsync(
                        $"ACESSO NEGADO: Usuário {usuarioId} tentou configurar WAHA da empresa {id} sem ser administrador",
                        nameof(ConfigurarWaha)
                    );

                    return Erro("Acesso negado. Você precisa ser administrador da empresa para realizar esta operação.");
                }

                await LogInfoAsync(
                    $"Permissão validada: Usuário {usuarioId} é admin da empresa {id}",
                    nameof(ConfigurarWaha)
                );

                // 5. Executar configuração/reconfiguração
                var resultado = await _provisionamentoService.ConfigurarOuReconfigurarWahaAsync(id, request);

                // 6. Registrar ação
                await RegistraAcaoAsync(
                    "Configurar/Reconfigurar WAHA",
                    SerializarParaLog(resultado.ConfiguracaoAnterior),
                    SerializarParaLog(resultado.ConfiguracaoAtual),
                    resultado.Sucesso
                        ? $"WAHA configurada para empresa {empresa.RazaoSocial} - Número: {request.NumeroWhatsApp}"
                        : $"Falha ao configurar WAHA: {resultado.Mensagem}"
                );

                // 7. Retornar resultado
                if (resultado.Sucesso)
                {
                    return Sucesso(
                        new
                        {
                            empresaId = id,
                            empresaNome = empresa.RazaoSocial,
                            configuracaoAnterior = resultado.ConfiguracaoAnterior,
                            configuracaoAtual = resultado.ConfiguracaoAtual,
                            acoes = resultado.Acoes,
                            logs = resultado.Logs
                        },
                        resultado.Mensagem
                    );
                }
                else
                {
                    return BadRequest(new
                    {
                        sucesso = false,
                        mensagem = resultado.Mensagem,
                        logs = resultado.Logs,
                        timestamp = DateTimeOffset.UtcNow
                    });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return await LogErroAsync(ex, nameof(ConfigurarWaha), "Usuário não autenticado");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ConfigurarWaha), $"EmpresaID: {id}, Request: {SerializarParaLog(request)}");
            }
        }

        #endregion
    }
}