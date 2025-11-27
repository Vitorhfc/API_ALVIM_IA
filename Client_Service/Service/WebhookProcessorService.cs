using Client_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Classes.Entidades.Client;
using Shared.Classes.Model;
using Shared.Helpers;
using Shared.Messaging.Models;
using System.Text.Json;

namespace Client_Service.Service
{
    public class WebhookProcessorService : IWebhookProcessorService
    {
        private readonly ITenantResolverService _tenantResolver;
        private readonly IClienteRepositorio _clienteRepository;
        private readonly IMensagemRepositorio _mensagemRepository;
        private readonly IAgrupamentoService _agrupamentoService;
        private readonly IProcessamentoIAService _processamentoIaService;
        private readonly IArquivoService _arquivoService;
        private readonly ILogClientService _logClientService;
        private readonly IClienteCadastroAutomaticoService _cadastroAutomaticoService;
        private readonly ILogger<WebhookProcessorService> _logger;

        public WebhookProcessorService(
            ITenantResolverService tenantResolver,
            IClienteRepositorio clienteRepository,
            IMensagemRepositorio mensagemRepository,
            IAgrupamentoService agrupamentoService,
            IProcessamentoIAService processamentoIaService,
            IArquivoService arquivoService,
            ILogClientService logClientService,
            IClienteCadastroAutomaticoService cadastroAutomaticoService,
            ILogger<WebhookProcessorService> logger)
        {
            _tenantResolver = tenantResolver;
            _clienteRepository = clienteRepository;
            _mensagemRepository = mensagemRepository;
            _agrupamentoService = agrupamentoService;
            _processamentoIaService = processamentoIaService;
            _arquivoService = arquivoService;
            _logClientService = logClientService;
            _cadastroAutomaticoService = cadastroAutomaticoService;
            _logger = logger;
        }

        #region VALIDAÇÕES
        public async Task<ValidacaoWebhookResult> ValidarWebhook(string payloadString, string empresaId)
        {
            // Validação 1: Payload vazio
            if (string.IsNullOrWhiteSpace(payloadString) || payloadString == "null")
            {
                return new ValidacaoWebhookResult
                {
                    EhValido = false,
                    DeveIgnorar = false,
                    TipoErro = "SAIDA_PAYLOAD_VAZIO",
                    MensagemErro = "Payload vazio ignorado"
                };
            }

            // Validação 2: Desserializar e validar payload
            WebhookWaHaRequest? webhookRequest;
            try
            {
                var payloadLimpo = JsonCleanerHelper.RemoverCampos(payloadString, CamposPesadosWaha).GetRawText();
                webhookRequest = JsonConvert.DeserializeObject<WebhookWaHaRequest>(payloadLimpo);

                if (webhookRequest == null)
                {
                    return new ValidacaoWebhookResult
                    {
                        EhValido = false,
                        DeveIgnorar = false,
                        TipoErro = "SAIDA_PAYLOAD_INVALIDO",
                        MensagemErro = "Payload inválido ignorado"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desserializar webhook - EmpresaId: {EmpresaId}", empresaId);
                return new ValidacaoWebhookResult
                {
                    EhValido = false,
                    DeveIgnorar = false,
                    TipoErro = "SAIDA_PAYLOAD_INVALIDO",
                    MensagemErro = "Payload inválido ignorado"
                };
            }

            // Validação 3: Verificar se deve ignorar a mensagem
            if (DeveIgnorarMensagem(webhookRequest))
            {
                return new ValidacaoWebhookResult
                {
                    EhValido = true,
                    DeveIgnorar = true,
                    TipoErro = null,
                    MensagemErro = "Mensagem ignorada",
                    WebhookRequest = webhookRequest
                };
            }

            // Validação bem-sucedida
            return new ValidacaoWebhookResult
            {
                EhValido = true,
                DeveIgnorar = false,
                TipoErro = null,
                MensagemErro = null,
                WebhookRequest = webhookRequest
            };
        }
        #endregion

        #region PROCESSAR WEBHOOK
        public async Task<ProcessamentoWebhookResult> ProcessarWebhook(
            WebhookWaHaRequest webhook,
            string empresaId,
            string idLog,
            CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(empresaId))
                    return CriarResultadoErro("Não foi possível identificar empresa");

                var tenantResolvido = await _tenantResolver.ResolverPorEmpresaIdAsync(empresaId, cancellationToken);
                if (!tenantResolvido)
                    return CriarResultadoErro($"Empresa não encontrada para o empresaId {empresaId}");

                var eventoType = webhook.@event?.ToLowerInvariant();

                _logger.LogInformation("Webhook processando - EmpresaId: {EmpresaId}, Evento: {Evento}, From: {From}",
                    empresaId, eventoType, webhook.payload?.from);

                return eventoType switch
                {
                    "message" => await ProcessarNovaMensagem(webhook, idLog, cancellationToken),
                    "message.any" => await ProcessarNovaMensagem(webhook, idLog, cancellationToken),
                    "message.ack" => await ProcessarStatusMensagem(webhook, idLog, cancellationToken),
                    "message.revoked" => await ProcessarMensagemDeletada(webhook, idLog, cancellationToken),
                    "message.edited" => await ProcessarMensagemEditada(webhook, idLog, cancellationToken),
                    "connection.state" => await ProcessarEventoConexao(webhook, idLog, cancellationToken),
                    _ => ProcessarEventoDesconhecido(webhook, eventoType)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar webhook - IdLog: {IdLog}", idLog);
                await _logClientService.RegistrarErro("WebhookProcessorService.ProcessarWebhook", ex.Message,
                    $"IdLog: {idLog}, Evento: {webhook.@event}", ex.StackTrace);

                return new ProcessamentoWebhookResult
                {
                    Sucesso = false,
                    Erro = "Erro interno ao processar webhook"
                };
            }
        }
        #region Processamento de Eventos

        private async Task<ProcessamentoWebhookResult> ProcessarNovaMensagem(
            WebhookWaHaRequest webhook,
            string idLog,
            CancellationToken cancellationToken)
        {
            try
            {
                var payload = webhook.payload;
                if (payload == null)
                    return CriarResultadoErro("Payload da mensagem está nulo");

                var webhookEvent = CriarEventoPadronizado(payload, webhook.@event);

                // Se fromMe é true (mensagem do usuário para cliente), apenas buscar cliente existente
                Cliente? cliente;
                if (payload.fromMe)
                {
                    var numeroInfo = Shared.Utils.TelefoneHelper.ExtrairInformacoes(webhookEvent.ContactInfo.PhoneNumber);
                    cliente = await _clienteRepository.BuscarPorNumeroWahaAsync(numeroInfo.NumeroWaha);

                    if (cliente == null)
                    {
                        _logger.LogWarning(
                            "Cliente não encontrado para mensagem fromMe - RecipientAlt: {RecipientAlt}, IdLog: {IdLog}",
                            payload.data?.Info?.RecipientAlt,
                            idLog
                        );
                        return CriarResultadoErro($"Cliente não encontrado para o número {numeroInfo.NumeroWaha}");
                    }

                    _logger.LogInformation(
                        "Cliente encontrado para mensagem fromMe - ClienteId: {ClienteId}, Número: {Numero}",
                        cliente.Id,
                        cliente.NumeroTelefoneWaha
                    );
                }
                else
                {
                    // Mensagem do cliente para o usuário: buscar ou criar
                    cliente = await _cadastroAutomaticoService.BuscarOuCriarClienteAsync(webhookEvent);
                }

                var arquivoId = await ProcessarMidiaSeNecessario(cliente.Id, payload, cancellationToken);
                var mensagem = CriarMensagem(cliente.Id, payload, arquivoId);

                await _mensagemRepository.AdicionarAsync(mensagem);

                _logger.LogInformation("Mensagem salva - Id: {MensagemId}, Cliente: {ClienteId}, Tipo: {Tipo}, FromMe: {FromMe}, FlgResponsavel: {FlgResponsavel}",
                    mensagem.Id, cliente.Id, mensagem.TipoMensagem, payload.fromMe, cliente.FlgRespostaResponsavel);

                await _clienteRepository.AtualizarUltimaInteracaoAsync(cliente.Id, DateTime.UtcNow);

                // Não processar mensagens fromMe (enviadas pelo responsável)
                if (payload.fromMe)
                {
                    _logger.LogInformation("Mensagem fromMe não será processada - Id: {MensagemId}", mensagem.Id);
                    return new ProcessamentoWebhookResult
                    {
                        Sucesso = true,
                        IdProcessamento = mensagem.Id,
                        Erro = null
                    };
                }

                // Não processar mensagens de clientes com flag de resposta por responsável ativa
                if (cliente.FlgRespostaResponsavel)
                {
                    _logger.LogInformation("Cliente com flag de resposta por responsável ativa - não será processado pela IA - ClienteId: {ClienteId}, MensagemId: {MensagemId}",
                        cliente.Id, mensagem.Id);
                    return new ProcessamentoWebhookResult
                    {
                        Sucesso = true,
                        IdProcessamento = mensagem.Id,
                        Erro = null
                    };
                }

                // Processar normalmente apenas se não for fromMe e não tiver flag ativa
                await _processamentoIaService.ProcessarMensagemAsync(mensagem.Id, cancellationToken);

                return new ProcessamentoWebhookResult
                {
                    Sucesso = true,
                    IdProcessamento = mensagem.Id,
                    Erro = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar nova mensagem - IdLog: {IdLog}", idLog);
                return CriarResultadoErro($"Erro ao processar mensagem: {ex.Message}");
            }
        }

        private async Task<ProcessamentoWebhookResult> ProcessarStatusMensagem(
            WebhookWaHaRequest webhook,
            string idLog,
            CancellationToken cancellationToken)
        {
            try
            {
                var payload = webhook.payload;
                if (payload?.ack == null)
                    return CriarResultadoErro("ACK não informado");

                var novoStatus = ConverterAckParaStatus(payload.ack.Value);
                await _mensagemRepository.AtualizarStatusAsync(payload.id, novoStatus);

                _logger.LogInformation("Status atualizado - Id: {Id}, Status: {Status}", payload.id, novoStatus);

                return new ProcessamentoWebhookResult
                {
                    Sucesso = true,
                    IdProcessamento = payload.id,
                    Erro = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar status - IdLog: {IdLog}", idLog);
                return CriarResultadoErro($"Erro ao atualizar status: {ex.Message}");
            }
        }

        private async Task<ProcessamentoWebhookResult> ProcessarMensagemDeletada(
            WebhookWaHaRequest webhook,
            string idLog,
            CancellationToken cancellationToken)
        {
            try
            {
                var payload = webhook.payload;
                await _mensagemRepository.MarcarComoDeletadaPorIdWhatsAppAsync(payload.id);

                _logger.LogInformation("Mensagem deletada - Id: {Id}", payload.id);

                return new ProcessamentoWebhookResult
                {
                    Sucesso = true,
                    IdProcessamento = payload.id,
                    Erro = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar deleção - IdLog: {IdLog}", idLog);
                return CriarResultadoErro($"Erro ao deletar mensagem: {ex.Message}");
            }
        }

        private async Task<ProcessamentoWebhookResult> ProcessarMensagemEditada(
            WebhookWaHaRequest webhook,
            string idLog,
            CancellationToken cancellationToken)
        {
            try
            {
                var payload = webhook.payload;
                await _mensagemRepository.AtualizarConteudoPorIdWhatsAppAsync(payload.id, payload.body);

                _logger.LogInformation("Mensagem editada - Id: {Id}", payload.id);

                return new ProcessamentoWebhookResult
                {
                    Sucesso = true,
                    IdProcessamento = payload.id,
                    Erro = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar edição - IdLog: {IdLog}", idLog);
                return CriarResultadoErro($"Erro ao atualizar mensagem editada: {ex.Message}");
            }
        }

        private async Task<ProcessamentoWebhookResult> ProcessarEventoConexao(
            WebhookWaHaRequest webhook,
            string idLog,
            CancellationToken cancellationToken)
        {
            try
            {
                await _logClientService.RegistrarInfo("WAHA", "Estado de conexão recebido", $"IdLog: {idLog}");
                _logger.LogInformation("Evento de conexão recebido - IdLog: {IdLog}", idLog);

                return new ProcessamentoWebhookResult
                {
                    Sucesso = true,
                    IdProcessamento = idLog,
                    Erro = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento de conexão - IdLog: {IdLog}", idLog);
                return CriarResultadoErro($"Erro ao processar evento: {ex.Message}");
            }
        }

        private ProcessamentoWebhookResult ProcessarEventoDesconhecido(WebhookWaHaRequest webhook, string? eventoType)
        {
            _logger.LogWarning("Evento desconhecido recebido: {Evento}", eventoType);
            return new ProcessamentoWebhookResult
            {
                Sucesso = true,
                IdProcessamento = Guid.NewGuid().ToString(),
                Erro = null
            };
        }

        #endregion
        #endregion

        #region Criação de Objetos

        private StandardWhatsAppEvent CriarEventoPadronizado(WebhookWaHaRequest.Payload payload, string? eventType)
        {
            // Quando fromMe é true, o payload.from é o número do atendente/empresa
            // O número real do cliente está em payload._data.Info.RecipientAlt
            string numeroCliente;
            string nomeCliente;

            if (payload.fromMe && !string.IsNullOrWhiteSpace(payload.data?.Info?.RecipientAlt))
            {
                // Mensagem enviada pelo usuário para o cliente
                numeroCliente = payload.data.Info.RecipientAlt;
                nomeCliente = "Cliente"; // Nome será obtido do banco de dados

                _logger.LogDebug(
                    "Mensagem fromMe detectada - Cliente (RecipientAlt): {RecipientAlt}, Atendente (from): {From}",
                    numeroCliente,
                    payload.from
                );
            }
            else
            {
                // Mensagem recebida do cliente
                numeroCliente = payload.from ?? string.Empty;
                nomeCliente = payload.notifyName ?? "Desconhecido";
            }

            return new StandardWhatsAppEvent
            {
                ContactInfo = new ContactInfo
                {
                    PhoneNumber = numeroCliente,
                    Name = nomeCliente,
                    IsGroup = numeroCliente?.Contains("@g.us") ?? false
                },
                MessageId = payload.id ?? string.Empty,
                Timestamp = payload.timestamp.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(payload.timestamp.Value).UtcDateTime
                    : DateTime.UtcNow,
                Body = payload.body ?? string.Empty,
                EventType = eventType ?? "message",
                FromMe = payload.fromMe
            };
        }

        private Mensagem CriarMensagem(string clienteId, WebhookWaHaRequest.Payload payload, string? arquivoId)
        {
            return new Mensagem
            {
                ClienteId = clienteId,
                IdMensagemWhatsApp = payload.id,
                TipoMensagem = DeterminarTipoMensagem(payload),
                Origem = payload.fromMe ? OrigemMensagem.Funcionario : OrigemMensagem.Cliente,
                FlgMensagemCliente = !payload.fromMe,
                ConteudoTexto = ExtrairConteudoTexto(payload),
                DtRecebido = DateTime.UtcNow,
                TimestampWhatsApp = payload.timestamp.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(payload.timestamp.Value).UtcDateTime
                    : DateTime.UtcNow,
                StatusEntrega = StatusEntrega.Entregue,
                FlgEnviadaAoN8N = false,
                Midia = CriarObjetoMidia(payload, arquivoId),
                Reacoes = new List<Shared.Classes.Entidades.Client.Reacao>()
            };
        }

        private MidiaInfo? CriarObjetoMidia(WebhookWaHaRequest.Payload payload, string? arquivoId)
        {
            if (payload.hasMedia != true)
                return null;

            return new MidiaInfo
            {
                ArquivoId = arquivoId,
                UrlDownload = payload.body,
                UrlLocal = null,
                NomeArquivo = null,
                MimeType = null,
                TamanhoBytes = null,
                Caption = null,
                FlgBaixada = arquivoId != null,
                DtDownload = arquivoId != null ? DateTime.UtcNow : null
            };
        }

        #endregion

        #region Processamento de Mídia

        private async Task<string?> ProcessarMidiaSeNecessario(
            string clienteId,
            WebhookWaHaRequest.Payload payload,
            CancellationToken cancellationToken)
        {
            if (payload.hasMedia != true || string.IsNullOrEmpty(payload.body))
                return null;

            _logger.LogInformation("Processando mídia - Cliente: {ClienteId}", clienteId);
            return await ProcessarMidia(clienteId, payload.id, payload.body, null, null, cancellationToken);
        }

        private async Task<string?> ProcessarMidia(
            string clienteId,
            string mensagemId,
            string mediaUrl,
            string? fileName,
            string? mimeType,
            CancellationToken cancellationToken)
        {
            try
            {
                var arquivo = await _arquivoService.SalvarArquivoAsync(
                    clienteId,
                    mensagemId,
                    mediaUrl,
                    fileName ?? "arquivo",
                    mimeType ?? "application/octet-stream"
                );

                return arquivo?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mídia - Cliente: {ClienteId}, Url: {Url}", clienteId, mediaUrl);
                return null;
            }
        }

        #endregion

        #region Métodos Auxiliares

        private TipoMensagem DeterminarTipoMensagem(WebhookWaHaRequest.Payload payload)
        {
            if (payload.hasMedia != true)
                return TipoMensagem.Texto;

            var type = payload.type?.ToLowerInvariant();
            if (string.IsNullOrEmpty(type))
                return TipoMensagem.Documento;

            if (type.Contains("image")) return TipoMensagem.Imagem;
            if (type.Contains("video")) return TipoMensagem.Video;
            if (type.Contains("audio") || type.Contains("ptt")) return TipoMensagem.Audio;
            if (type.Contains("document")) return TipoMensagem.Documento;
            if (type.Contains("sticker")) return TipoMensagem.Sticker;

            return TipoMensagem.Documento;
        }

        private string? ExtrairConteudoTexto(WebhookWaHaRequest.Payload payload)
        {
            if (string.IsNullOrEmpty(payload.body) || payload.hasMedia == true)
                return null;

            return payload.body;
        }

        private StatusEntrega ConverterAckParaStatus(int ack)
        {
            return ack switch
            {
                1 => StatusEntrega.Enviada,
                2 => StatusEntrega.Entregue,
                3 => StatusEntrega.Lida,
                _ => StatusEntrega.Enviada
            };
        }

        private ProcessamentoWebhookResult CriarResultadoErro(string mensagem)
        {
            return new ProcessamentoWebhookResult
            {
                Sucesso = false,
                IdProcessamento = null,
                Erro = mensagem
            };
        }

        private bool DeveIgnorarMensagem(WebhookWaHaRequest request)
        {
            if (request.@event == "session.status") return true;
            if (request.payload?.from?.EndsWith("@broadcast") ?? false) return true;
            if (request.payload?.from?.EndsWith("@g.us") ?? false) return true;
            // Removido: if (request.payload?.fromMe ?? false) return true;
            // Mensagens fromMe agora serão salvas no banco, mas não processadas

            // WAHA dispara 2 eventos para mensagens de clientes: "message" e "message.any"
            // Ignorar "message.any" quando fromMe é false para evitar processamento duplicado
            // Apenas "message" será processado para mensagens de clientes
            if (request.@event == "message.any" && !(request.payload?.fromMe ?? false))
            {
                _logger.LogDebug(
                    "Ignorando evento message.any duplicado (fromMe: false) - From: {From}",
                    request.payload?.from
                );
                return true;
            }

            return false;
        }

        #endregion

        #region Configurações

        private static readonly string[] CamposPesadosWaha =
        [
            "payload._data.RawMessage",
            "payload.mediaUrl",
            "payload._data.Message.videoMessage.JPEGThumbnail",
            "payload._data.Message.videoMessage.fileEncSHA256",
            "payload._data.Message.videoMessage.thumbnailEncSHA256",
            "payload._data.Message.videoMessage.streamingSidecar",
            "payload._data.Message.videoMessage.directPath",
            "payload._data.Message.videoMessage.thumbnailDirectPath",
            "payload._data.Message.videoMessage.messageContextInfo",
            "payload._data.Message.videoMessage.mediaKey",
            "payload._data.Message.videoMessage.fileSHA256",
            "payload._data.Message.videoMessage.thumbnailSHA256",
            "payload._data.Message.documentMessage.JPEGThumbnail",
            "payload._data.Message.documentMessage.thumbnailEncSHA256",
            "payload._data.Message.documentMessage.thumbnailDirectPath",
            "payload._data.Message.documentMessage.thumbnailSHA256",
            "payload._data.Message.documentMessage.fileSHA256",
            "payload._data.Message.documentMessage.fileEncSHA256",
            "payload._data.Message.documentMessage.mediaKey",
            "payload._data.Message.documentMessage.directPath",
            "payload._data.Message.extendedTextMessage.JPEGThumbnail",
            "payload._data.Message.extendedTextMessage.thumbnailEncSHA256",
            "payload._data.Message.extendedTextMessage.thumbnailDirectPath",
            "payload._data.Message.extendedTextMessage.thumbnailSHA256",
            "payload._data.Message.extendedTextMessage.mediaKey",
            "payload._data.Message.extendedTextMessage.faviconMMSMetadata",
            "payload._data.Message.imageMessage.JPEGThumbnail",
            "payload._data.Message.imageMessage.fileSHA256",
            "payload._data.Message.imageMessage.fileEncSHA256",
            "payload._data.Message.imageMessage.directPath",
            "payload._data.Message.imageMessage.mediaKey",
            "payload._data.Message.imageMessage.scansSidecar",
            "payload._data.Message.imageMessage.scanLengths",
            "payload._data.Message.imageMessage.midQualityFileSHA256",
            "payload._data.Message.audioMessage.fileEncSHA256",
            "payload._data.Message.audioMessage.fileSHA256",
            "payload._data.Message.audioMessage.mediaKey",
            "payload._data.Message.audioMessage.directPath",
            "payload._data.Message.stickerMessage.fileEncSHA256",
            "payload._data.Message.stickerMessage.fileSHA256",
            "payload._data.Message.stickerMessage.mediaKey",
            "payload._data.Message.stickerMessage.directPath",
            "payload._data.Message.stickerMessage.JPEGThumbnail"
        ];

        #endregion
    }
}