using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.ModelView.Client;

namespace Cliente.Controllers
{
    /// <summary>
    /// Controller para operações de atendimento via WhatsApp
    /// Permite que atendentes enviem mensagens, reajam e controlem o modo de resposta (IA/Humano)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AtendimentoWhatsAppController : ControllerBase
    {
        private readonly IAtendimentoWhatsAppService _atendimentoService;
        private readonly ILogger<AtendimentoWhatsAppController> _logger;

        public AtendimentoWhatsAppController(
            IAtendimentoWhatsAppService atendimentoService,
            ILogger<AtendimentoWhatsAppController> logger)
        {
            _atendimentoService = atendimentoService;
            _logger = logger;
        }

        /// <summary>
        /// Envia mensagem de texto para um cliente
        /// </summary>
        /// <param name="request">Dados da mensagem</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPost("mensagem/texto")]
        [ProducesResponseType(typeof(AtendimentoWhatsAppResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EnviarMensagemTexto(
            [FromBody] EnviarMensagemTextoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Requisição de envio de mensagem de texto - ClienteId: {ClienteId}",
                    request?.ClienteId
                );

                if (request == null)
                    return BadRequest(new { mensagem = "Request inválido" });

                var resultado = await _atendimentoService.EnviarMensagemTextoAsync(request, cancellationToken);

                if (!resultado.Sucesso)
                    return BadRequest(resultado);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar envio de mensagem de texto");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro interno ao processar requisição",
                    erro = ex.Message
                });
            }
        }

        /// <summary>
        /// Envia mensagem com mídia (imagem, vídeo, documento) para um cliente
        /// </summary>
        /// <param name="request">Dados da mídia</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPost("mensagem/midia")]
        [ProducesResponseType(typeof(AtendimentoWhatsAppResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EnviarMensagemMidia(
            [FromBody] EnviarMensagemMidiaRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Requisição de envio de mídia - ClienteId: {ClienteId}, Tipo: {Tipo}",
                    request?.ClienteId,
                    request?.TipoMidia
                );

                if (request == null)
                    return BadRequest(new { mensagem = "Request inválido" });

                var resultado = await _atendimentoService.EnviarMensagemMidiaAsync(request, cancellationToken);

                if (!resultado.Sucesso)
                    return BadRequest(resultado);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar envio de mídia");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro interno ao processar requisição",
                    erro = ex.Message
                });
            }
        }

        /// <summary>
        /// Envia áudio/voice note para um cliente
        /// </summary>
        /// <param name="request">Dados do áudio</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPost("mensagem/audio")]
        [ProducesResponseType(typeof(AtendimentoWhatsAppResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EnviarAudio(
            [FromBody] EnviarAudioRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Requisição de envio de áudio - ClienteId: {ClienteId}",
                    request?.ClienteId
                );

                if (request == null)
                    return BadRequest(new { mensagem = "Request inválido" });

                var resultado = await _atendimentoService.EnviarAudioAsync(request, cancellationToken);

                if (!resultado.Sucesso)
                    return BadRequest(resultado);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar envio de áudio");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro interno ao processar requisição",
                    erro = ex.Message
                });
            }
        }

        /// <summary>
        /// Reage a uma mensagem com emoji
        /// </summary>
        /// <param name="request">Dados da reação</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPost("mensagem/reagir")]
        [ProducesResponseType(typeof(AtendimentoWhatsAppResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReagirMensagem(
            [FromBody] ReagirMensagemRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Requisição de reação - MensagemId: {MensagemId}, Emoji: {Emoji}",
                    request?.MensagemId,
                    request?.Emoji
                );

                if (request == null)
                    return BadRequest(new { mensagem = "Request inválido" });

                var resultado = await _atendimentoService.ReagirMensagemAsync(request, cancellationToken);

                if (!resultado.Sucesso)
                    return BadRequest(resultado);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar reação");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro interno ao processar requisição",
                    erro = ex.Message
                });
            }
        }

        /// <summary>
        /// Remove uma mensagem enviada
        /// </summary>
        /// <param name="request">ID da mensagem</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpDelete("mensagem")]
        [ProducesResponseType(typeof(AtendimentoWhatsAppResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoverMensagem(
            [FromBody] RemoverMensagemRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Requisição de remoção de mensagem - MensagemId: {MensagemId}",
                    request?.MensagemId
                );

                if (request == null)
                    return BadRequest(new { mensagem = "Request inválido" });

                var resultado = await _atendimentoService.RemoverMensagemAsync(request, cancellationToken);

                if (!resultado.Sucesso)
                    return BadRequest(resultado);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar remoção de mensagem");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro interno ao processar requisição",
                    erro = ex.Message
                });
            }
        }

        /// <summary>
        /// Edita uma mensagem enviada
        /// </summary>
        /// <param name="request">Dados da edição</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPut("mensagem")]
        [ProducesResponseType(typeof(AtendimentoWhatsAppResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditarMensagem(
            [FromBody] EditarMensagemRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Requisição de edição de mensagem - MensagemId: {MensagemId}",
                    request?.MensagemId
                );

                if (request == null)
                    return BadRequest(new { mensagem = "Request inválido" });

                var resultado = await _atendimentoService.EditarMensagemAsync(request, cancellationToken);

                if (!resultado.Sucesso)
                    return BadRequest(resultado);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar edição de mensagem");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro interno ao processar requisição",
                    erro = ex.Message
                });
            }
        }

        /// <summary>
        /// Alterna o modo de resposta entre IA e atendimento humano
        /// Quando ativado o atendimento humano, a IA para de responder automaticamente
        /// </summary>
        /// <param name="request">Dados da alteração</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPost("modo-resposta/alternar")]
        [ProducesResponseType(typeof(AtendimentoWhatsAppResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AlternarModoResposta(
            [FromBody] AlternarModoRespostaRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Requisição de alteração de modo de resposta - ClienteId: {ClienteId}, AtendimentoHumano: {AtendimentoHumano}",
                    request?.ClienteId,
                    request?.AtendimentoHumano
                );

                if (request == null)
                    return BadRequest(new { mensagem = "Request inválido" });

                var resultado = await _atendimentoService.AlternarModoRespostaAsync(request, cancellationToken);

                if (!resultado.Sucesso)
                    return BadRequest(resultado);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar alteração de modo de resposta");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro interno ao processar requisição",
                    erro = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtém o status atual do modo de resposta de um cliente
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Status do modo de resposta</returns>
        [HttpGet("modo-resposta/status/{clienteId}")]
        [ProducesResponseType(typeof(StatusModoRespostaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterStatusModoResposta(
            [FromRoute] string clienteId,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Requisição de status do modo de resposta - ClienteId: {ClienteId}",
                    clienteId
                );

                if (string.IsNullOrWhiteSpace(clienteId))
                    return BadRequest(new { mensagem = "ClienteId é obrigatório" });

                var status = await _atendimentoService.ObterStatusModoRespostaAsync(clienteId, cancellationToken);

                if (status == null)
                    return NotFound(new { mensagem = "Cliente não encontrado" });

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter status do modo de resposta - ClienteId: {ClienteId}", clienteId);
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro interno ao processar requisição",
                    erro = ex.Message
                });
            }
        }

        /// <summary>
        /// Atalho para ativar atendimento humano (desativa IA)
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPost("modo-resposta/{clienteId}/ativar-atendimento-humano")]
        [ProducesResponseType(typeof(AtendimentoWhatsAppResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AtivarAtendimentoHumano(
            [FromRoute] string clienteId,
            CancellationToken cancellationToken)
        {
            var request = new AlternarModoRespostaRequest
            {
                ClienteId = clienteId,
                AtendimentoHumano = true
            };

            return await AlternarModoResposta(request, cancellationToken);
        }

        /// <summary>
        /// Atalho para desativar atendimento humano (ativa IA)
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPost("modo-resposta/{clienteId}/ativar-ia")]
        [ProducesResponseType(typeof(AtendimentoWhatsAppResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AtivarIA(
            [FromRoute] string clienteId,
            CancellationToken cancellationToken)
        {
            var request = new AlternarModoRespostaRequest
            {
                ClienteId = clienteId,
                AtendimentoHumano = false
            };

            return await AlternarModoResposta(request, cancellationToken);
        }
    }
}
