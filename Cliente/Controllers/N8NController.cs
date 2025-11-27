using Client.Controllers.Base;
using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Shared.Classes.Model;

namespace Client.Controllers
{
    /// <summary>
    /// Controller para integração com N8N - Endpoints específicos para automações
    /// </summary>
    [ApiController]
    [Route("api/n8n")]
    [Authorize]
    public class N8NController : ControllerBaseClient<N8NController>
    {
        #region Campos

        private readonly IN8NService _n8nService;
        private readonly ILogClientService _logClientService;
        private readonly ILogger<N8NController> _logger;
        private readonly Client_Repository.Configuration.Contexto.Interface.IContextoMultiTenantService _contextoMultiTenant;

        #endregion

        #region Construtor

        public N8NController(
            IN8NService n8nService,
            ILogClientService logClientService,
            ILogger<N8NController> logger,
            Client_Repository.Configuration.Contexto.Interface.IContextoMultiTenantService contextoMultiTenant)
            : base(logClientService, logger)
        {
            _n8nService = n8nService;
            _logClientService = logClientService;
            _logger = logger;
            _contextoMultiTenant = contextoMultiTenant;
        }

        #endregion

        #region Endpoints

        /// <summary>
        /// Obtém os planos/features habilitados para um cliente específico
        /// </summary>
        /// <param name="idCliente">ID do cliente</param>
        /// <returns>Objeto com flags dos planos habilitados</returns>
        [HttpGet("cliente/{idCliente}/plano")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterPlanoCliente(string idCliente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idCliente))
                    return Erro("ID do cliente é obrigatório");

                // Buscar empresa do cliente para configurar contexto multi-tenant
                var empresaId = await _contextoMultiTenant.BuscarEmpresaPorClienteIdAsync(idCliente);

                if (string.IsNullOrWhiteSpace(empresaId))
                {
                    _logger.LogWarning(
                        "Não foi possível encontrar IdEmpresa para o IdCliente: {IdCliente}",
                        idCliente
                    );
                    return Erro($"Não foi possível identificar a empresa do cliente {idCliente}");
                }

                // Buscar usuário ativo da empresa
                var usuarioId = await _contextoMultiTenant.BuscarUsuarioAtivoDaEmpresaAsync(empresaId);

                if (string.IsNullOrWhiteSpace(usuarioId))
                {
                    _logger.LogWarning(
                        "Não foi possível encontrar usuário ativo para a empresa: {EmpresaId}",
                        empresaId
                    );
                    return Erro($"Não foi possível encontrar usuário ativo para a empresa");
                }

                // Configurar contexto multi-tenant com usuário real
                await _contextoMultiTenant.ConfigurarContextoUsuarioAsync(usuarioId, empresaId);

                var plano = await _n8nService.ObterPlanoClienteAsync(idCliente);

                await LogInfoAsync(
                    $"Plano obtido para cliente: {idCliente}",
                    nameof(ObterPlanoCliente)
                );

                return Sucesso(plano, "Plano do cliente obtido com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ObterPlanoCliente), $"IdCliente: {idCliente}");
            }
        }

        /// <summary>
        /// Obtém o contexto completo do projeto configurado para a empresa
        /// </summary>
        /// <param name="idEmpresa">ID da empresa</param>
        /// <returns>Contexto do projeto em formato texto</returns>
        [HttpGet("empresa/{idEmpresa}/contexto-projeto")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterContextoProjeto(string idEmpresa)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idEmpresa))
                    return Erro("ID da empresa é obrigatório");

                // Buscar usuário ativo da empresa
                var usuarioId = await _contextoMultiTenant.BuscarUsuarioAtivoDaEmpresaAsync(idEmpresa);

                if (string.IsNullOrWhiteSpace(usuarioId))
                {
                    _logger.LogWarning(
                        "Não foi possível encontrar usuário ativo para a empresa: {EmpresaId}",
                        idEmpresa
                    );
                    return Erro($"Não foi possível encontrar usuário ativo para a empresa");
                }

                // Configurar contexto multi-tenant com usuário real
                await _contextoMultiTenant.ConfigurarContextoUsuarioAsync(usuarioId, idEmpresa);

                var contexto = await _n8nService.ObterContextoProjetoAsync(idEmpresa);

                await LogInfoAsync(
                    $"Contexto do projeto obtido para empresa: {idEmpresa}",
                    nameof(ObterContextoProjeto)
                );

                return Sucesso(contexto, "Contexto do projeto obtido com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ObterContextoProjeto), $"IdEmpresa: {idEmpresa}");
            }
        }

        /// <summary>
        /// Obtém as mensagens dos últimos 2 dias de um cliente específico
        /// </summary>
        /// <param name="idCliente">ID do cliente</param>
        /// <returns>Lista de mensagens dos últimos 2 dias</returns>
        [HttpGet("cliente/{idCliente}/mensagens-recentes")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterMensagensRecentes(string idCliente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idCliente))
                    return Erro("ID do cliente é obrigatório");

                // Buscar empresa do cliente para configurar contexto multi-tenant
                var empresaId = await _contextoMultiTenant.BuscarEmpresaPorClienteIdAsync(idCliente);

                if (string.IsNullOrWhiteSpace(empresaId))
                {
                    _logger.LogWarning(
                        "Não foi possível encontrar IdEmpresa para o IdCliente: {IdCliente}",
                        idCliente
                    );
                    return Erro($"Não foi possível identificar a empresa do cliente {idCliente}");
                }

                // Buscar usuário ativo da empresa
                var usuarioId = await _contextoMultiTenant.BuscarUsuarioAtivoDaEmpresaAsync(empresaId);

                if (string.IsNullOrWhiteSpace(usuarioId))
                {
                    _logger.LogWarning(
                        "Não foi possível encontrar usuário ativo para a empresa: {EmpresaId}",
                        empresaId
                    );
                    return Erro($"Não foi possível encontrar usuário ativo para a empresa");
                }

                // Configurar contexto multi-tenant com usuário real
                await _contextoMultiTenant.ConfigurarContextoUsuarioAsync(usuarioId, empresaId);

                var mensagens = await _n8nService.ObterMensagensRecentesAsync(idCliente);

                await LogInfoAsync(
                    $"Mensagens recentes obtidas para cliente: {idCliente}, Total: {mensagens.Count}",
                    nameof(ObterMensagensRecentes)
                );

                return Sucesso(mensagens, $"{mensagens.Count} mensagem(ns) dos últimos 2 dias encontrada(s)");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ObterMensagensRecentes), $"IdCliente: {idCliente}");
            }
        }

        /// <summary>
        /// Obtém a lista de documentos configurados pelo cliente
        /// </summary>
        /// <param name="idCliente">ID do cliente</param>
        /// <returns>Lista de documentos disponíveis</returns>
        [HttpGet("cliente/{idCliente}/documentos")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterDocumentos(string idCliente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idCliente))
                    return Erro("ID do cliente é obrigatório");

                // Buscar empresa do cliente para configurar contexto multi-tenant
                var empresaId = await _contextoMultiTenant.BuscarEmpresaPorClienteIdAsync(idCliente);

                if (string.IsNullOrWhiteSpace(empresaId))
                {
                    _logger.LogWarning(
                        "Não foi possível encontrar IdEmpresa para o IdCliente: {IdCliente}",
                        idCliente
                    );
                    return Erro($"Não foi possível identificar a empresa do cliente {idCliente}");
                }

                // Buscar usuário ativo da empresa
                var usuarioId = await _contextoMultiTenant.BuscarUsuarioAtivoDaEmpresaAsync(empresaId);

                if (string.IsNullOrWhiteSpace(usuarioId))
                {
                    _logger.LogWarning(
                        "Não foi possível encontrar usuário ativo para a empresa: {EmpresaId}",
                        empresaId
                    );
                    return Erro($"Não foi possível encontrar usuário ativo para a empresa");
                }

                // Configurar contexto multi-tenant com usuário real
                await _contextoMultiTenant.ConfigurarContextoUsuarioAsync(usuarioId, empresaId);

                var documentos = await _n8nService.ObterDocumentosAsync(idCliente);

                await LogInfoAsync(
                    $"Documentos obtidos para cliente: {idCliente}, Total: {documentos.Documentos?.Count ?? 0}",
                    nameof(ObterDocumentos)
                );

                return Sucesso(documentos, $"{documentos.Documentos?.Count ?? 0} documento(s) encontrado(s)");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ObterDocumentos), $"IdCliente: {idCliente}");
            }
        }

        /// <summary>
        /// [NOVO FLUXO ASSÍNCRONO] Recebe callback do N8N com a resposta processada da IA
        /// Este endpoint recebe as mensagens processadas pelo N8N após agrupamento e processamento pela IA
        /// </summary>
        /// <param name="request">Callback com as mensagens processadas pela IA</param>
        /// <returns>Resultado do processamento</returns>
        [HttpPost("callback")]
        [AllowAnonymous]

        public async Task<IActionResult> callback([FromBody] N8NCallbackRespostaRequest request)
        {
            ReceberCallback(request);
            return Ok();
        }

        [HttpPost("callbackInterno")]
        [AllowAnonymous]
        public async Task<IActionResult> ReceberCallback([FromBody] N8NCallbackRespostaRequest request)
        {

            try
            {
                _logger.LogInformation(
                    "Callback recebido do N8N - Cliente: {IdCliente}, Empresa: {IdEmpresa}",
                    request?.idCliente,
                    request?.idEmpresa
                );

                if (request == null)
                    return Erro("Dados da requisição são obrigatórios");

                if (string.IsNullOrWhiteSpace(request.idCliente))
                    return Erro("IdCliente é obrigatório");

                // Se IdEmpresa não foi fornecido, buscar automaticamente através do IdCliente
                if (string.IsNullOrWhiteSpace(request.idEmpresa))
                {
                    _logger.LogInformation(
                        "IdEmpresa não fornecido. Buscando através do IdCliente: {IdCliente}",
                        request.idCliente
                    );

                    var empresaId = await _contextoMultiTenant.BuscarEmpresaPorClienteIdAsync(request.idCliente);

                    if (string.IsNullOrWhiteSpace(empresaId))
                    {
                        _logger.LogWarning(
                            "Não foi possível encontrar IdEmpresa para o IdCliente: {IdCliente}",
                            request.idCliente
                        );
                        return Erro($"Não foi possível identificar a empresa do cliente {request.idCliente}. " +
                                   "O cliente precisa ser cadastrado primeiro ou você pode fornecer o IdEmpresa no request.");
                    }

                    request.idEmpresa = empresaId;

                    _logger.LogInformation(
                        "IdEmpresa encontrado automaticamente: {IdEmpresa} para IdCliente: {IdCliente}",
                        empresaId,
                        request.idCliente
                    );
                }

                if (request.output == null)
                    return Erro("Output é obrigatório");

                if (request.output.mensagens == null || !request.output.mensagens.Any())
                    return Erro("Lista de mensagens no output é obrigatória");

                await _n8nService.ProcessarCallbackRespostaAsync(request);

                await RegistraAcaoAsync(
                    "Callback N8N",
                    SerializarParaLog(request),
                    null,
                    $"Callback processado - Cliente: {request.idCliente}, Mensagens: {request.output.mensagens.Count}"
                );

                _logger.LogInformation(
                    "Callback do N8N processado com sucesso - Cliente: {IdCliente}",
                    request.idCliente
                );

                return Sucesso(new
                {
                    sucesso = true,
                    mensagem = "Callback processado com sucesso",
                    qtdMensagens = request.output.mensagens.Count
                }, "Callback do N8N processado com sucesso");
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "Erro de validação ao processar callback do N8N");
                return Erro(argEx.Message);
            }
            catch (KeyNotFoundException notFoundEx)
            {
                _logger.LogWarning(notFoundEx, "Cliente não encontrado ao processar callback");
                return NaoEncontrado(notFoundEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar callback do N8N - Cliente: {IdCliente}", request?.idCliente);
                return LogErroAsync(
                    ex,
                    nameof(ReceberCallback),
                    $"IdCliente: {request?.idCliente}, IdEmpresa: {request?.idEmpresa}"
                );
            }
        }
        #endregion
    }
}
