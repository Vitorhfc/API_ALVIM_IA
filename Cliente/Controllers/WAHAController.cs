using Client.Controllers.Base;
using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.Model;
using Shared.Services.Interface;
using Admin_Service.Service.Interface;

namespace Client.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de sessões WAHA (WhatsApp HTTP API)
    /// </summary>
    [ApiController]
    [Route("api/whatsapp")]
    [Authorize]
    public class WAHAController : ControllerBaseClient<WAHAController>
    {
        #region Campos

        private readonly IWAHAService _wahaService;
        private readonly ILogClientService _logClientService;
        private readonly IWhatsAppNotificationService _notificationService;
        private readonly ILogger<WAHAController> _logger;
        private readonly IEmpresaService _empresaService;

        #endregion

        #region Construtor

        public WAHAController(
            IWAHAService wahaService,
            ILogClientService logClientService,
            IWhatsAppNotificationService notificationService,
            ILogger<WAHAController> logger,
            IEmpresaService empresaService)
            : base(logClientService, logger)
        {
            _wahaService = wahaService;
            _logClientService = logClientService;
            _notificationService = notificationService;
            _logger = logger;
            _empresaService = empresaService;
        }

        #endregion

        #region Endpoints de Gerenciamento de Sessão

        /// <summary>
        /// Inicia uma nova sessão do WhatsApp e retorna o QR Code para escaneamento
        /// </summary>
        /// <param name="sessionName">Nome único da sessão (recomendado usar o ID do cliente)</param>
        /// <returns>QR Code para escaneamento no WhatsApp</returns>
        /// <remarks>
        /// Este endpoint inicia uma nova sessão WAHA. Após iniciar, o QR Code deve ser escaneado
        /// no WhatsApp do dispositivo móvel em até 30 segundos.
        /// </remarks>
        [HttpPost("sessao/{sessionName}/iniciar")]
        [ProducesResponseType(typeof(WAHAQRCodeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> IniciarSessao(string sessionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                    return Erro("Nome da sessão é obrigatório");

                _logger.LogInformation("Iniciando sessão WAHA: {SessionName}", sessionName);

                var resultado = await _wahaService.IniciarSessaoAsync(sessionName);

                await LogInfoAsync(
                    $"Sessão WAHA iniciada: {sessionName}",
                    nameof(IniciarSessao)
                );

                // Notifica via SignalR se QR Code foi gerado
                if (!string.IsNullOrEmpty(resultado.QRCode))
                {
                    await _notificationService.NotificarQRCodeGeradoAsync(sessionName, resultado.QRCode);
                    await _notificationService.NotificarStatusAlteradoAsync(
                        sessionName,
                        "SCAN_QR_CODE",
                        "QR Code gerado. Escaneie no WhatsApp."
                    );
                }
                
                await Task.Delay(3000);

                // Verificar status da sessão e atualizar credenciais no banco se conectada
                try
                {
                    var status = await _wahaService.ObterStatusSessaoAsync(sessionName);

                    if (status.EstaConectado)
                    {
                        // Obter informações da conta para capturar o número
                        var accountInfo = await _wahaService.ObterInformacoesContaAsync(sessionName);

                        // Buscar empresa pelo sessionName
                        var empresa = await _empresaService.BuscarPorSessionNameAsync(sessionName);

                        if (empresa != null)
                        {
                            // Atualizar credenciais WAHA
                            empresa.WahaSessionName = sessionName;
                            empresa.WahaNumeroWhatsApp = accountInfo.NumeroFormatado;
                            empresa.FlgWahaAtivo = true;
                            empresa.WahaDataConexao = DateTime.UtcNow;
                            empresa.WahaUltimaVerificacao = DateTime.UtcNow;

                            await _empresaService.EditarAsync(empresa);

                            _logger.LogInformation(
                                "Credenciais WAHA atualizadas para empresa {EmpresaId}: {Numero}",
                                empresa.Id,
                                accountInfo.NumeroFormatado
                            );

                            await _notificationService.NotificarStatusAlteradoAsync(
                                sessionName,
                                "WORKING",
                                "Sessão conectada com sucesso!"
                            );
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Empresa não encontrada para sessionName: {SessionName}",
                                sessionName
                            );
                        }
                    }
                }
                catch (Exception exCredenciais)
                {
                    _logger.LogError(
                        exCredenciais,
                        "Erro ao atualizar credenciais WAHA para sessão: {SessionName}",
                        sessionName
                    );
                    // Não falha o processo, apenas registra o erro
                }

                return Sucesso(resultado, "Sessão iniciada com sucesso. Escaneie o QR Code no WhatsApp.");
            }
            catch (Exception ex)
            {
                // Notifica erro via SignalR
                await _notificationService.NotificarStatusAlteradoAsync(
                    sessionName,
                    "FAILED",
                    $"Erro ao iniciar sessão: {ex.Message}"
                );

                return LogErroAsync(ex, nameof(IniciarSessao), $"SessionName: {sessionName}");
            }
        }

        /// <summary>
        /// Obtém o QR Code de uma sessão existente
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <returns>QR Code da sessão</returns>
        [HttpGet("sessao/{sessionName}/qrcode")]
        [ProducesResponseType(typeof(WAHAQRCodeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObterQRCode(string sessionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                    return Erro("Nome da sessão é obrigatório");

                var resultado = await _wahaService.ObterQRCodeAsync(sessionName);

                return Sucesso(resultado, "QR Code obtido com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ObterQRCode), $"SessionName: {sessionName}");
            }
        }

        /// <summary>
        /// Verifica o status de uma sessão WAHA
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <returns>Status atual da sessão</returns>
        /// <remarks>
        /// Possíveis status:
        /// - STARTING: Sessão iniciando
        /// - SCAN_QR_CODE: Aguardando escaneamento do QR Code
        /// - WORKING: Sessão conectada e funcionando
        /// - FAILED: Sessão falhou
        /// - STOPPED: Sessão parada
        /// </remarks>
        [HttpGet("sessao/{sessionName}/status")]
        [ProducesResponseType(typeof(WAHAStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObterStatus(string sessionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                    return Erro("Nome da sessão é obrigatório");

                var resultado = await _wahaService.ObterStatusSessaoAsync(sessionName);

                return Sucesso(resultado, "Status da sessão obtido com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ObterStatus), $"SessionName: {sessionName}");
            }
        }

        /// <summary>
        /// Para/desconecta uma sessão WAHA ativa
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <returns>Resultado da operação</returns>
        [HttpPost("sessao/{sessionName}/parar")]
        [ProducesResponseType(typeof(WAHAActionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PararSessao(string sessionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                    return Erro("Nome da sessão é obrigatório");

                _logger.LogInformation("Parando sessão WAHA: {SessionName}", sessionName);

                var resultado = await _wahaService.PararSessaoAsync(sessionName);

                await LogInfoAsync(
                    $"Sessão WAHA parada: {sessionName}",
                    nameof(PararSessao)
                );

                // Notifica via SignalR que a sessão foi parada
                await _notificationService.NotificarWhatsAppDesconectadoAsync(
                    sessionName,
                    "Sessão parada pelo usuário"
                );

                await _notificationService.NotificarStatusAlteradoAsync(
                    sessionName,
                    "STOPPED",
                    "Sessão parada com sucesso"
                );

                return Sucesso(resultado, "Sessão parada com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(PararSessao), $"SessionName: {sessionName}");
            }
        }

        /// <summary>
        /// Remove completamente uma sessão WAHA
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <returns>Resultado da operação</returns>
        /// <remarks>
        /// ATENÇÃO: Esta operação remove permanentemente a sessão e todos os seus dados.
        /// Não é possível recuperar a sessão após a remoção.
        /// </remarks>
        [HttpDelete("sessao/{sessionName}")]
        [ProducesResponseType(typeof(WAHAActionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoverSessao(string sessionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                    return Erro("Nome da sessão é obrigatório");

                _logger.LogWarning("Removendo sessão WAHA: {SessionName}", sessionName);

                var resultado = await _wahaService.RemoverSessaoAsync(sessionName);

                await LogInfoAsync(
                    $"Sessão WAHA removida: {sessionName}",
                    nameof(RemoverSessao)
                );

                // Notifica via SignalR que a sessão foi removida
                await _notificationService.NotificarWhatsAppDesconectadoAsync(
                    sessionName,
                    "Sessão removida"
                );

                await _notificationService.NotificarStatusAlteradoAsync(
                    sessionName,
                    "STOPPED",
                    "Sessão removida com sucesso"
                );

                return Sucesso(resultado, "Sessão removida com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(RemoverSessao), $"SessionName: {sessionName}");
            }
        }

        /// <summary>
        /// Reinicia uma sessão WAHA existente
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <returns>Resultado da operação</returns>
        [HttpPost("sessao/{sessionName}/reiniciar")]
        [ProducesResponseType(typeof(WAHAActionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReiniciarSessao(string sessionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                    return Erro("Nome da sessão é obrigatório");

                _logger.LogInformation("Reiniciando sessão WAHA: {SessionName}", sessionName);

                var resultado = await _wahaService.ReiniciarSessaoAsync(sessionName);

                await LogInfoAsync(
                    $"Sessão WAHA reiniciada: {sessionName}",
                    nameof(ReiniciarSessao)
                );

                return Sucesso(resultado, "Sessão reiniciada com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ReiniciarSessao), $"SessionName: {sessionName}");
            }
        }

        /// <summary>
        /// Lista todas as sessões WAHA disponíveis
        /// </summary>
        /// <returns>Lista de sessões</returns>
        [HttpGet("sessoes")]
        [ProducesResponseType(typeof(List<WAHASessionInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ListarSessoes()
        {
            try
            {
                var sessoes = await _wahaService.ListarSessoesAsync();

                await LogInfoAsync(
                    $"Total de sessões listadas: {sessoes.Count}",
                    nameof(ListarSessoes)
                );

                return Sucesso(sessoes, $"{sessoes.Count} sessão(ões) encontrada(s)");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarSessoes));
            }
        }

        /// <summary>
        /// Obtém informações da conta conectada em uma sessão
        /// </summary>
        /// <param name="sessionName">Nome da sessão</param>
        /// <returns>Informações da conta</returns>
        [HttpGet("sessao/{sessionName}/conta")]
        [ProducesResponseType(typeof(WAHAAccountInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObterInformacoesConta(string sessionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                    return Erro("Nome da sessão é obrigatório");

                var informacoes = await _wahaService.ObterInformacoesContaAsync(sessionName);

                return Sucesso(informacoes, "Informações da conta obtidas com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ObterInformacoesConta), $"SessionName: {sessionName}");
            }
        }

        /// <summary>
        /// Gera um nome de sessão no padrão Alvim_{empresaId}_{ddMMyyyy}
        /// </summary>
        /// <param name="empresaId">ID da empresa</param>
        /// <returns>Nome da sessão gerado</returns>
        [HttpGet("sessao/gerar-nome/{empresaId}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GerarNomeSessao(string empresaId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(empresaId))
                    return Erro("ID da empresa é obrigatório");

                var nomeSessao = _wahaService.GerarNomeSessao(empresaId);

                return Sucesso(nomeSessao, "Nome da sessão gerado com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(GerarNomeSessao), $"EmpresaId: {empresaId}");
            }
        }

        /// <summary>
        /// Gera um nome de sessão e salva no banco de dados da empresa
        /// Se a empresa já tiver um nome de sessão, retorna o existente sem sobrescrever
        /// </summary>
        /// <param name="empresaId">ID da empresa</param>
        /// <returns>Nome da sessão gerado e salvo (ou o existente)</returns>
        /// <remarks>
        /// Este endpoint é idempotente: se chamado múltiplas vezes para a mesma empresa,
        /// retorna sempre o mesmo nome de sessão sem gerar um novo.
        /// </remarks>
        [HttpPost("sessao/gerar-e-salvar-nome/{empresaId}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GerarESalvarNomeSessao(string empresaId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(empresaId))
                    return Erro("ID da empresa é obrigatório");

                var nomeSessao = await _wahaService.GerarESalvarNomeSessaoAsync(empresaId);

                await LogInfoAsync(
                    $"Nome de sessão obtido: {nomeSessao} para empresa: {empresaId}",
                    nameof(GerarESalvarNomeSessao)
                );

                return Sucesso(nomeSessao, "Nome da sessão obtido com sucesso");
            }
            catch (InvalidOperationException ex)
            {
                return Erro(ex.Message, StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(GerarESalvarNomeSessao), $"EmpresaId: {empresaId}");
            }
        }

        #endregion

    }
}
