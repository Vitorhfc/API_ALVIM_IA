using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Shared.Classes.Model;
using Shared.Services.Interface;
using Shared.Utils;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Shared.Services
{
    /// <summary>
    /// Serviço genérico para envio de mensagens WhatsApp via WAHA API
    /// </summary>
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(ILogger<WhatsAppService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        #region Envio de Mensagens

        public async Task<EnvioResponse> EnviarMensagemAsync(
            EnviarWhatsAppRequest request,
            string wahaApiUrl,
            string wahaApiKey)
        {
            try
            {
                // Validações
                if (string.IsNullOrWhiteSpace(request?.NumeroDestino))
                {
                    return CriarRespostaErro("Número de destino é obrigatório");
                }

                if (string.IsNullOrWhiteSpace(request.Mensagem))
                {
                    return CriarRespostaErro("Mensagem é obrigatória");
                }

                if (string.IsNullOrWhiteSpace(request.Session))
                {
                    return CriarRespostaErro("Session é obrigatória");
                }

                if (string.IsNullOrWhiteSpace(wahaApiUrl))
                {
                    return CriarRespostaErro("URL da API WAHA não configurada");
                }

                // Formatar número (adicionar @c.us se necessário)
                var numeroFormatado = FormatarNumeroWhatsApp(request.NumeroDestino);

                var number = await FetchNumberIdAsync(request.Session, numeroFormatado, wahaApiUrl, wahaApiKey, CancellationToken.None);


                // Configurar headers - padrão WAHA API
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                if (!string.IsNullOrWhiteSpace(wahaApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("X-Api-Key", wahaApiKey);
                }

                object payload;
                string endpoint;

                // Criar payload baseado no tipo de mensagem
                if (request.Tipo == TipoMensagemWhatsApp.Texto)
                {
                    // Payload para mensagem de texto (formato WAHA API)
                    payload = new
                    {
                        session = request.Session,
                        chatId = numeroFormatado,
                        text = request.Mensagem,
                        reply_to = !string.IsNullOrEmpty(request.QuotedMessageId)
                            ? BuildQuotedMessageId(request.QuotedMessageId)
                            : null
                    };

                    // Endpoint para texto: /api/sendText
                    endpoint = "/api/sendText";
                }
                else
                {
                    // Mensagem com mídia
                    if (string.IsNullOrWhiteSpace(request.UrlMidia))
                    {
                        return CriarRespostaErro("URL da mídia é obrigatória para este tipo de mensagem");
                    }

                    // Determinar mimetype baseado no tipo
                    var mimetype = ObterMimeType(request.Tipo, request.NomeArquivo);

                    // Payload para mídia
                    payload = new
                    {
                        session = request.Session,
                        chatId = numeroFormatado,
                        caption = request.Mensagem,
                        file = new
                        {
                            mimetype = mimetype,
                            url = request.UrlMidia,
                            filename = request.NomeArquivo ?? $"arquivo.{ObterExtensao(request.Tipo)}"
                        },
                        reply_to = !string.IsNullOrEmpty(request.QuotedMessageId)
                            ? BuildQuotedMessageId(request.QuotedMessageId)
                            : null
                    };

                    // Endpoint para mídia
                    endpoint = $"/api/{ObterEndpointMidia(request.Tipo)}";
                }

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Construir URL completa
                var baseUrl = wahaApiUrl.TrimEnd('/');
                // Remove /api do final se existir
                if (baseUrl.EndsWith("/api"))
                {
                    baseUrl = baseUrl.Substring(0, baseUrl.Length - 4);
                }
                var url = $"{baseUrl}{endpoint}";

                _logger.LogInformation(
                    "Enviando mensagem WhatsApp - Para: {Numero}, Tipo: {Tipo}, Session: {Session}, URL: {Url}",
                    numeroFormatado,
                    request.Tipo,
                    request.Session,
                    url
                );

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Mensagem WhatsApp enviada com sucesso - Para: {Numero}, Session: {Session}",
                        numeroFormatado,
                        request.Session
                    );

                    // Tentar extrair ID da mensagem da resposta
                    string? messageId = null;
                    try
                    {
                        var responseObj = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        // Formato WAHA: response._data.Info.ID ou response.id
                        messageId = responseObj?._data?.Info?.ID?.ToString() ?? responseObj?.id?.ToString();
                    }
                    catch { }

                    return new EnvioResponse
                    {
                        Sucesso = true,
                        Mensagem = "Mensagem WhatsApp enviada com sucesso",
                        IdEnvio = messageId,
                        DataEnvio = DateTime.UtcNow
                    };
                }
                else
                {
                    var erro = $"Erro ao enviar mensagem: {response.StatusCode} - {responseContent}";
                    _logger.LogWarning(erro);

                    return new EnvioResponse
                    {
                        Sucesso = false,
                        Mensagem = "Falha ao enviar mensagem WhatsApp",
                        DataEnvio = DateTime.UtcNow,
                        Erro = erro
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem WhatsApp");

                return new EnvioResponse
                {
                    Sucesso = false,
                    Mensagem = "Erro ao processar envio de mensagem WhatsApp",
                    DataEnvio = DateTime.UtcNow,
                    Erro = ex.Message
                };
            }
        }

        public async Task<EnvioResponse> EnviarTextoAsync(
            string numeroDestino,
            string mensagem,
            string session,
            string wahaApiUrl,
            string wahaApiKey)
        {
            return await EnviarMensagemAsync(
                new EnviarWhatsAppRequest
                {
                    NumeroDestino = numeroDestino,
                    Mensagem = mensagem,
                    Session = session,
                    Tipo = TipoMensagemWhatsApp.Texto
                },
                wahaApiUrl,
                wahaApiKey
            );
        }

        public async Task<EnvioResponse> EnviarMidiaAsync(
            string numeroDestino,
            string mensagem,
            string urlMidia,
            string session,
            TipoMensagemWhatsApp tipo,
            string wahaApiUrl,
            string wahaApiKey)
        {
            return await EnviarMensagemAsync(
                new EnviarWhatsAppRequest
                {
                    NumeroDestino = numeroDestino,
                    Mensagem = mensagem,
                    Session = session,
                    UrlMidia = urlMidia,
                    Tipo = tipo
                },
                wahaApiUrl,
                wahaApiKey
            );
        }

        /// <summary>
        /// Envia áudio/voice note
        /// </summary>
        public async Task<EnvioResponse> EnviarAudioAsync(
            string numeroDestino,
            string urlAudio,
            string session,
            string wahaApiUrl,
            string wahaApiKey,
            string? quotedMessageId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(numeroDestino))
                    return CriarRespostaErro("Número de destino é obrigatório");

                if (string.IsNullOrWhiteSpace(urlAudio))
                    return CriarRespostaErro("URL do áudio é obrigatória");

                var numeroFormatado = FormatarNumeroWhatsApp(numeroDestino);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                if (!string.IsNullOrWhiteSpace(wahaApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("X-Api-Key", wahaApiKey);
                }

                var payload = new
                {
                    session = session,
                    chatId = numeroFormatado,
                    file = new
                    {
                        mimetype = "audio/ogg; codecs=opus",
                        url = urlAudio
                    },
                    convert = true,
                    reply_to = !string.IsNullOrEmpty(quotedMessageId)
                        ? BuildQuotedMessageId(quotedMessageId)
                        : null
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{wahaApiUrl.TrimEnd('/')}/api/sendVoice";

                _logger.LogInformation("Enviando áudio WhatsApp - Para: {Numero}", numeroFormatado);

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string? messageId = null;
                    try
                    {
                        var responseObj = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        messageId = responseObj?._data?.Info?.ID?.ToString();
                    }
                    catch { }

                    return new EnvioResponse
                    {
                        Sucesso = true,
                        Mensagem = "Áudio enviado com sucesso",
                        IdEnvio = messageId,
                        DataEnvio = DateTime.UtcNow
                    };
                }

                return new EnvioResponse
                {
                    Sucesso = false,
                    Mensagem = "Falha ao enviar áudio",
                    Erro = $"{response.StatusCode} - {responseContent}",
                    DataEnvio = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar áudio");
                return CriarRespostaErro(ex.Message);
            }
        }

        #endregion

        #region Gerenciamento de Sessões

        /// <summary>
        /// Cria ou atualiza uma sessão WAHA com configuração completa
        /// PUT /api/sessions/{name}
        /// </summary>
        public async Task<CreateWahaSessionResponse?> UpsertSessionAsync(
            CreateWahaSessionRequest request,
            string wahaApiUrl,
            string wahaApiKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.name))
                {
                    _logger.LogWarning("Nome da sessão é obrigatório");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(wahaApiUrl))
                {
                    _logger.LogWarning("URL da API WAHA não configurada");
                    return null;
                }

                // WAHA API endpoint: PUT /api/sessions/{name}
                var endpoint = $"{wahaApiUrl.TrimEnd('/')}/api/sessions/{request.name}";

                _logger.LogInformation(
                    "Criando/atualizando sessão WAHA: {SessionName} em {Url}",
                    request.name,
                    endpoint
                );

                var response = await HttpClientHelper.SendRequestAsync(
                    method: Method.PUT,
                    endpoint: endpoint,
                    data: request,
                    headers: BuildHeader(wahaApiKey),
                    cancellationToken: cancellationToken
                );

                if (response?.Content != null && response.IsSuccessful)
                {
                    var sessionResponse = JsonConvert.DeserializeObject<CreateWahaSessionResponse>(response.Content);

                    _logger.LogInformation(
                        "Sessão WAHA criada/atualizada: {SessionName}, Status: {Status}",
                        sessionResponse?.name,
                        sessionResponse?.status
                    );

                    return sessionResponse;
                }
                else
                {
                    _logger.LogWarning(
                        "Erro ao criar/atualizar sessão WAHA: {StatusCode} - {Content}",
                        response?.StatusCode,
                        response?.Content
                    );
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exceção ao criar/atualizar sessão WAHA: {SessionName}", request?.name);
                return null;
            }
        }

        /// <summary>
        /// Inicia uma sessão WAHA existente
        /// POST /api/sessions/{name}/start
        /// </summary>
        public async Task<CreateWahaSessionResponse?> StartSessionAsync(
            string sessionName,
            string wahaApiUrl,
            string wahaApiKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                {
                    _logger.LogWarning("Nome da sessão é obrigatório");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(wahaApiUrl))
                {
                    _logger.LogWarning("URL da API WAHA não configurada");
                    return null;
                }

                // WAHA API endpoint: POST /api/sessions/{sessionName}/start
                var endpoint = $"{wahaApiUrl.TrimEnd('/')}/api/sessions/{sessionName}/start";

                _logger.LogInformation(
                    "Iniciando sessão WAHA: {SessionName} em {Url}",
                    sessionName,
                    endpoint
                );

                var response = await HttpClientHelper.SendRequestAsync(
                    method: Method.POST,
                    endpoint: endpoint,
                    data: null, // Body vazio conforme documentação WAHA
                    headers: BuildHeader(wahaApiKey),
                    cancellationToken: cancellationToken
                );

                if (response?.Content != null && response.IsSuccessful)
                {
                    var sessionResponse = JsonConvert.DeserializeObject<CreateWahaSessionResponse>(response.Content);

                    _logger.LogInformation(
                        "Sessão WAHA iniciada: {SessionName}, Status: {Status}",
                        sessionResponse?.name,
                        sessionResponse?.status
                    );

                    return sessionResponse;
                }
                else
                {
                    _logger.LogWarning(
                        "Erro ao iniciar sessão WAHA: {StatusCode} - {Content}",
                        response?.StatusCode,
                        response?.Content
                    );
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exceção ao iniciar sessão WAHA: {SessionName}", sessionName);
                return null;
            }
        }

        /// <summary>
        /// Cria uma nova sessão WAHA completa (Upsert + Start)
        /// </summary>
        public async Task<CreateWahaSessionResponse?> CreateSessionAsync(
            CreateWahaSessionRequest request,
            string wahaApiUrl,
            string wahaApiKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Criando sessão WAHA completa: {SessionName}",
                    request?.name
                );

                // 1. Criar/atualizar sessão com configuração
                var upsertResult = await UpsertSessionAsync(request, wahaApiUrl, wahaApiKey, cancellationToken);

                if (upsertResult == null)
                {
                    _logger.LogWarning("Falha ao criar/atualizar sessão WAHA");
                    return null;
                }

                // 2. Se start=true, iniciar a sessão
                if (request?.start == true)
                {
                    _logger.LogInformation("Iniciando sessão WAHA: {SessionName}", request.name);

                    var startResult = await StartSessionAsync(request.name, wahaApiUrl, wahaApiKey, cancellationToken);

                    if (startResult != null)
                    {
                        return startResult;
                    }

                    // Se falhou ao iniciar, retornar resultado do upsert
                    _logger.LogWarning("Sessão criada mas falhou ao iniciar. Retornando resultado do upsert.");
                }

                return upsertResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exceção ao criar sessão WAHA completa: {SessionName}", request?.name);
                return null;
            }
        }

        public async Task<bool> RestartSessionAsync(
            string instanceName,
            string wahaApiUrl,
            string wahaApiKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(instanceName))
                {
                    _logger.LogWarning("Nome da instância não fornecido para restart");
                    return false;
                }

                // Configurar headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", wahaApiKey);

                var url = $"{wahaApiUrl.TrimEnd('/')}/api/sessions/{instanceName}/restart";

                _logger.LogInformation(
                    "Reiniciando sessão WAHA: {InstanceName}",
                    instanceName
                );

                var response = await _httpClient.PostAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Sessão reiniciada com sucesso: {InstanceName}",
                        instanceName
                    );
                    return true;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Erro ao reiniciar sessão: {StatusCode} - {Content}",
                        response.StatusCode,
                        responseContent
                    );
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reiniciar sessão: {InstanceName}", instanceName);
                return false;
            }
        }

        #endregion

        #region Status e QR Code

        public async Task<WAHAStatusResponse> GetInstanceStatusAsync(
            string instanceName,
            string wahaApiUrl,
            string wahaApiKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(instanceName))
                {
                    return new WAHAStatusResponse
                    {
                        Sucesso = false,
                        Mensagem = "Nome da instância é obrigatório",
                        Erro = "Nome da instância não fornecido"
                    };
                }

                // Configurar headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", wahaApiKey);

                var url = $"{wahaApiUrl.TrimEnd('/')}/api/sessions/{instanceName}";

                _logger.LogInformation(
                    "Verificando status da instância WAHA: {InstanceName}",
                    instanceName
                );

                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    dynamic? responseObj = null;
                    try
                    {
                        responseObj = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    }
                    catch { }

                    var status = responseObj?.status?.ToString() ?? "UNKNOWN";
                    var isConnected = status.Equals("WORKING", StringComparison.OrdinalIgnoreCase);

                    return new WAHAStatusResponse
                    {
                        Sucesso = true,
                        Status = status,
                        IsConnected = isConnected,
                        Mensagem = $"Status da instância: {status}"
                    };
                }
                else
                {
                    var erro = $"Erro ao obter status: {response.StatusCode} - {responseContent}";
                    _logger.LogWarning(erro);

                    return new WAHAStatusResponse
                    {
                        Sucesso = false,
                        Status = "ERROR",
                        IsConnected = false,
                        Mensagem = "Falha ao obter status da instância",
                        Erro = erro
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter status da instância: {InstanceName}", instanceName);

                return new WAHAStatusResponse
                {
                    Sucesso = false,
                    Status = "ERROR",
                    IsConnected = false,
                    Mensagem = "Erro ao processar verificação de status",
                    Erro = ex.Message
                };
            }
        }

        public async Task<WAHAQrCodeResponse> GetQrCodeAsync(
            string instanceName,
            string wahaApiUrl,
            string wahaApiKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(instanceName))
                {
                    return new WAHAQrCodeResponse
                    {
                        Sucesso = false,
                        Mensagem = "Nome da instância é obrigatório",
                        Erro = "Nome da instância não fornecido"
                    };
                }

                // Primeiro verificar status
                var statusResponse = await GetInstanceStatusAsync(instanceName, wahaApiUrl, wahaApiKey);

                if (statusResponse.IsConnected)
                {
                    return new WAHAQrCodeResponse
                    {
                        Sucesso = true,
                        Status = "CONNECTED",
                        Mensagem = "Instância já está conectada"
                    };
                }

                // Se não estiver conectado e não estiver em SCAN_QR_CODE, reiniciar
                if (statusResponse.Status?.ToUpper() != "SCAN_QR_CODE")
                {
                    await RestartSessionAsync(instanceName, wahaApiUrl, wahaApiKey);
                    // Aguardar um momento para a sessão reiniciar
                    await Task.Delay(2000);
                }

                // Obter QR Code
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", wahaApiKey);

                var url = $"{wahaApiUrl.TrimEnd('/')}/api/{instanceName}/auth/qr?format=image";

                _logger.LogInformation(
                    "Obtendo QR Code para instância: {InstanceName}",
                    instanceName
                );

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    var base64Image = Convert.ToBase64String(imageBytes);

                    _logger.LogInformation(
                        "QR Code obtido com sucesso para instância: {InstanceName}",
                        instanceName
                    );

                    return new WAHAQrCodeResponse
                    {
                        Sucesso = true,
                        QrCodeBase64 = base64Image,
                        Status = "SCAN_QR_CODE",
                        Mensagem = "QR Code gerado com sucesso"
                    };
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var erro = $"Erro ao obter QR Code: {response.StatusCode} - {responseContent}";
                    _logger.LogWarning(erro);

                    return new WAHAQrCodeResponse
                    {
                        Sucesso = false,
                        Status = statusResponse.Status ?? "",
                        Mensagem = "Falha ao obter QR Code",
                        Erro = erro
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter QR Code para instância: {InstanceName}", instanceName);

                return new WAHAQrCodeResponse
                {
                    Sucesso = false,
                    Mensagem = "Erro ao processar obtenção de QR Code",
                    Erro = ex.Message
                };
            }
        }

        #endregion

        #region Webhook

        public async Task<bool> SetWebhookAsync(
            string instanceName,
            string webhookUrl,
            string wahaApiUrl,
            string wahaApiKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(instanceName) || string.IsNullOrWhiteSpace(webhookUrl))
                {
                    _logger.LogWarning("Nome da instância ou URL do webhook não fornecidos");
                    return false;
                }

                // Configurar headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", wahaApiKey);

                var payload = new
                {
                    url = webhookUrl,
                    events = new[]
                    {
                        "message",
                        "message.ack",
                        "message.any",
                        "message.reaction",
                        "message.revoked",
                        "message.edited",
                        "connection.state"
                    },
                    hmac = (string?)null,
                    retries = 3,
                    customHeaders = new List<object>()
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{wahaApiUrl.TrimEnd('/')}/api/{instanceName}/settings/webhooks";

                _logger.LogInformation(
                    "Configurando webhook para instância {InstanceName}: {WebhookUrl}",
                    instanceName,
                    webhookUrl
                );

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Webhook configurado com sucesso para instância: {InstanceName}",
                        instanceName
                    );
                    return true;
                }
                else
                {
                    _logger.LogWarning(
                        "Erro ao configurar webhook: {StatusCode} - {Content}",
                        response.StatusCode,
                        responseContent
                    );
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao configurar webhook para instância: {InstanceName}", instanceName);
                return false;
            }
        }

        #endregion

        #region Listar Sessões

        /// <summary>
        /// Lista todas as sessões disponíveis na API WAHA
        /// GET /api/sessions?all={true|false}
        /// </summary>
        public async Task<List<WAHASessionInfo>> ListarSessionsAsync(
            string wahaApiUrl,
            string wahaApiKey,
            bool incluirTodas = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(wahaApiUrl))
                {
                    _logger.LogWarning("ListarSessionsAsync: URL WAHA não configurada");
                    return new List<WAHASessionInfo>();
                }

                // Configurar headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                if (!string.IsNullOrWhiteSpace(wahaApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("X-Api-Key", wahaApiKey);
                }

                // Construir URL
                var baseUrl = wahaApiUrl.TrimEnd('/');
                if (baseUrl.EndsWith("/api"))
                {
                    baseUrl = baseUrl[..^4];
                }
                var url = $"{baseUrl}/api/sessions?all={incluirTodas.ToString().ToLower()}";

                _logger.LogInformation("Listando sessões WAHA - URL: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var sessions = JsonConvert.DeserializeObject<List<WAHASessionInfo>>(responseContent);

                    _logger.LogInformation(
                        "Sessões WAHA listadas com sucesso - Total: {Total}",
                        sessions?.Count ?? 0
                    );

                    return sessions ?? new List<WAHASessionInfo>();
                }
                else
                {
                    _logger.LogWarning(
                        "Erro ao listar sessões: {StatusCode} - {Content}",
                        response.StatusCode,
                        responseContent
                    );
                    return new List<WAHASessionInfo>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar sessões WAHA");
                return new List<WAHASessionInfo>();
            }
        }

        #endregion

        #region Reações

        public async Task<EnvioResponse> EnviarReacaoAsync(
            string idMensagem,
            string emoji,
            string session,
            string wahaApiUrl,
            string wahaApiKey)
        {
            try
            {
                // Validações detalhadas com logs
                if (string.IsNullOrWhiteSpace(idMensagem))
                {
                    _logger.LogWarning("EnviarReacaoAsync: ID da mensagem vazio ou nulo");
                    return CriarRespostaErro("ID da mensagem é obrigatório");
                }

                if (string.IsNullOrWhiteSpace(emoji))
                {
                    _logger.LogWarning("EnviarReacaoAsync: Emoji vazio ou nulo. IdMensagem: {IdMsg}", idMensagem);
                    return CriarRespostaErro("Emoji é obrigatório");
                }

                if (string.IsNullOrWhiteSpace(session))
                {
                    _logger.LogWarning("EnviarReacaoAsync: Session vazio ou nulo");
                    return CriarRespostaErro("Session é obrigatória");
                }

                if (string.IsNullOrWhiteSpace(wahaApiUrl))
                {
                    _logger.LogWarning("EnviarReacaoAsync: URL WAHA não configurada");
                    return CriarRespostaErro("URL da API WAHA não configurada");
                }

                // Configurar headers - X-Api-Key format
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                if (!string.IsNullOrWhiteSpace(wahaApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("X-Api-Key", wahaApiKey);
                }

                // Payload para reação (formato WAHA API)
                var payload = new
                {
                    messageId = idMensagem,
                    reaction = emoji,
                    session = session
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Endpoint para reação: PUT /api/reaction
                var baseUrl = wahaApiUrl.TrimEnd('/');
                // Remove /api do final se existir
                if (baseUrl.EndsWith("/api"))
                {
                    baseUrl = baseUrl[..^4];
                }
                var url = $"{baseUrl}/api/reaction";

                _logger.LogInformation(
                    "Enviando reação WhatsApp - Mensagem: {IdMensagem}, Emoji: {Emoji}, Session: {Session}, URL: {Url}",
                    idMensagem,
                    emoji,
                    session,
                    url
                );

                // Usar PUT em vez de POST para reações
                var response = await _httpClient.PutAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Reação WhatsApp enviada com sucesso - Mensagem: {IdMensagem}, Session: {Session}",
                        idMensagem,
                        session
                    );

                    return new EnvioResponse
                    {
                        Sucesso = true,
                        Mensagem = "Reação WhatsApp enviada com sucesso",
                        DataEnvio = DateTime.UtcNow
                    };
                }
                else
                {
                    var erro = $"Erro ao enviar reação: {response.StatusCode} - {responseContent}";
                    _logger.LogWarning(erro);

                    return new EnvioResponse
                    {
                        Sucesso = false,
                        Mensagem = "Falha ao enviar reação WhatsApp",
                        DataEnvio = DateTime.UtcNow,
                        Erro = erro
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar reação WhatsApp - IdMsg: {IdMsg}, Emoji: {Emoji}, Session: {Session}",
                    idMensagem, emoji, session);

                return new EnvioResponse
                {
                    Sucesso = false,
                    Mensagem = "Erro ao processar envio de reação WhatsApp",
                    DataEnvio = DateTime.UtcNow,
                    Erro = ex.Message
                };
            }
        }

        #endregion

        #region Validação e Verificação

        /// <summary>
        /// Valida se um número existe no WhatsApp usando WAHA API
        /// GET /api/contacts/check-exists?phone={phone}&session={session}
        /// </summary>
        public async Task<FetchNumberIdResult> FetchNumberIdAsync(
            string instanceName,
            string phone,
            string apiUrl,
            string apiKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var phoneDigits = TelefoneHelper.ExtrairDigitos(phone);
                var endpoint = $"{apiUrl.TrimEnd('/')}/api/contacts/check-exists?phone={phoneDigits}&session={instanceName}";

                var response = await HttpClientHelper.SendRequestAsync(
                    method: Method.GET,
                    endpoint: endpoint,
                    headers: BuildHeader(apiKey),
                    cancellationToken: cancellationToken
                );

                var result = response?.Content != null
                    ? JsonConvert.DeserializeObject<WhatsappNumbersResponse>(response.Content)
                    : null;

                if (response.IsSuccessful && result != null)
                {
                    _logger.LogInformation(
                        "Número validado - Phone: {Phone}, Exists: {Exists}, ChatId: {ChatId}",
                        phoneDigits,
                        result.numberExists,
                        result.chatId
                    );

                    return new FetchNumberIdResult("success", result.numberExists, result.chatId);
                }

                _logger.LogWarning(
                    "Erro ao validar número - Phone: {Phone}, Status: {Status}",
                    phoneDigits,
                    response?.StatusCode
                );

                return new FetchNumberIdResult("error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar número do WhatsApp: {Phone}", phone);
                return new FetchNumberIdResult("error");
            }
        }

        /// <summary>
        /// Obtém URL da foto de perfil
        /// GET /api/contacts/profile-picture?contactId={contactId}&refresh={bool}&session={session}
        /// </summary>
        public async Task<string?> GetProfileImageUrlAsync(
            string instanceName,
            string phoneOrId,
            string apiUrl,
            string apiKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoint = $"{apiUrl.TrimEnd('/')}/api/contacts/profile-picture?contactId={phoneOrId}&refresh=false&session={instanceName}";

                var response = await HttpClientHelper.SendRequestAsync(
                    method: Method.GET,
                    endpoint: endpoint,
                    headers: BuildHeader(apiKey),
                    cancellationToken: cancellationToken
                );

                if (response?.IsSuccessful == true && !string.IsNullOrEmpty(response.Content))
                {
                    using var document = System.Text.Json.JsonDocument.Parse(response.Content);
                    var root = document.RootElement;

                    if (root.TryGetProperty("profilePictureURL", out var urlElement))
                    {
                        return urlElement.GetString();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter foto de perfil: {PhoneOrId}", phoneOrId);
                return null;
            }
        }

        #endregion

        #region Gerenciamento de Mensagens

        /// <summary>
        /// Remove uma mensagem para todos os participantes
        /// DELETE /api/{instance}/chats/{chatId}/messages/{messageId}
        /// </summary>
        public async Task<bool> RemoveMessageForAllAsync(
            string instanceName,
            string phoneOrId,
            string messageId,
            string apiUrl,
            string apiKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validar número se necessário
                var (chatId, _) = await HandlerFetchNumberIdAsync(
                    instanceName,
                    phoneOrId,
                    apiUrl,
                    apiKey,
                    cancellationToken);

                if (string.IsNullOrEmpty(chatId))
                {
                    _logger.LogWarning("Não foi possível obter chatId para remover mensagem");
                    return false;
                }

                // Formato: {fromMe}_{chat}_{message_id}
                var fullMessageId = $"true_{chatId}_{messageId}";
                var endpoint = $"{apiUrl.TrimEnd('/')}/api/{instanceName}/chats/{chatId}/messages/{fullMessageId}";

                var response = await HttpClientHelper.SendRequestAsync(
                    method: Method.DELETE,
                    endpoint: endpoint,
                    headers: BuildHeader(apiKey),
                    cancellationToken: cancellationToken
                );

                if (response.IsSuccessful)
                {
                    _logger.LogInformation(
                        "Mensagem removida com sucesso - MessageId: {MessageId}, ChatId: {ChatId}",
                        messageId,
                        chatId
                    );
                    return true;
                }

                _logger.LogWarning(
                    "Falha ao remover mensagem - Status: {Status}, Content: {Content}",
                    response.StatusCode,
                    response.Content
                );

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover mensagem: {MessageId}", messageId);
                return false;
            }
        }

        /// <summary>
        /// Edita uma mensagem enviada
        /// PUT /api/{instance}/chats/{chatId}/messages/{messageId}
        /// </summary>
        public async Task<bool> EditMessageAsync(
            string instanceName,
            string phoneOrId,
            string messageId,
            string editedMessage,
            string apiUrl,
            string apiKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validar número se necessário
                var (chatId, _) = await HandlerFetchNumberIdAsync(
                    instanceName,
                    phoneOrId,
                    apiUrl,
                    apiKey,
                    cancellationToken);

                if (string.IsNullOrEmpty(chatId))
                {
                    _logger.LogWarning("Não foi possível obter chatId para editar mensagem");
                    return false;
                }

                // Formato: {fromMe}_{chat}_{message_id}
                var fullMessageId = $"true_{chatId}_{messageId}";
                var endpoint = $"{apiUrl.TrimEnd('/')}/api/{instanceName}/chats/{chatId}/messages/{fullMessageId}";

                var body = new
                {
                    text = editedMessage,
                    linkPreview = true,
                    linkPreviewHighQuality = false
                };

                var response = await HttpClientHelper.SendRequestAsync(
                    method: Method.PUT,
                    endpoint: endpoint,
                    data: body,
                    headers: BuildHeader(apiKey),
                    cancellationToken: cancellationToken
                );

                if (response.IsSuccessful)
                {
                    _logger.LogInformation(
                        "Mensagem editada com sucesso - MessageId: {MessageId}, ChatId: {ChatId}",
                        messageId,
                        chatId
                    );
                    return true;
                }

                _logger.LogWarning(
                    "Falha ao editar mensagem - Status: {Status}, Content: {Content}",
                    response.StatusCode,
                    response.Content
                );

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar mensagem: {MessageId}", messageId);
                return false;
            }
        }

        #endregion

        #region Grupos

        ///// <summary>
        ///// Lista grupos da instância
        ///// GET /api/{instance}/groups
        ///// </summary>
        //public async Task<List<GroupInfo>> FetchGroupsAsync(
        //    string instanceName,
        //    string apiUrl,
        //    string apiKey,
        //    CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        var endpoint = $"{apiUrl.TrimEnd('/')}/api/{instanceName}/groups";

        //        var response = await HttpClientHelper.SendRequestAsync(
        //            method: Method.GET,
        //            endpoint: endpoint,
        //            headers: BuildHeader(apiKey),
        //            cancellationToken: cancellationToken
        //        );

        //        if (response?.IsSuccessful == true && !string.IsNullOrEmpty(response.Content))
        //        {
        //            var groups = JsonConvert.DeserializeObject<List<GroupInfo>>(response.Content);
        //            return groups ?? new List<GroupInfo>();
        //        }

        //        return new List<GroupInfo>();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao listar grupos");
        //        return new List<GroupInfo>();
        //    }
        //}

        ///// <summary>
        ///// Obtém informações de um grupo específico
        ///// GET /api/{instance}/groups/{groupJid}
        ///// </summary>
        //public async Task<GroupInfo?> FindGroupInfoAsync(
        //    string instanceName,
        //    string groupJid,
        //    string apiUrl,
        //    string apiKey,
        //    CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        var endpoint = $"{apiUrl.TrimEnd('/')}/api/{instanceName}/groups/{groupJid}";

        //        var response = await HttpClientHelper.SendRequestAsync(
        //            method: Method.GET,
        //            endpoint: endpoint,
        //            headers: BuildHeader(apiKey),
        //            cancellationToken: cancellationToken
        //        );

        //        if (response?.IsSuccessful == true && !string.IsNullOrEmpty(response.Content))
        //        {
        //            var groupInfo = JsonConvert.DeserializeObject<GroupInfo>(response.Content);
        //            return groupInfo;
        //        }

        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao obter informações do grupo: {GroupJid}", groupJid);
        //        return null;
        //    }
        //}

        ///// <summary>
        ///// Cria um novo grupo
        ///// POST /api/{instance}/groups
        ///// </summary>
        //public async Task<GroupCreateResult?> CreateGroupAsync(
        //    string instanceName,
        //    string name,
        //    string[] participants,
        //    string apiUrl,
        //    string apiKey,
        //    CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        // Validar participantes
        //        var validParticipants = new List<string>();

        //        foreach (var participant in participants)
        //        {
        //            var result = await FetchNumberIdAsync(instanceName, participant, apiUrl, apiKey, cancellationToken);
        //            if (!string.IsNullOrEmpty(result.NumberId))
        //            {
        //                validParticipants.Add(result.NumberId);
        //            }
        //        }

        //        if (validParticipants.Count == 0)
        //        {
        //            _logger.LogWarning("Nenhum participante válido para criar grupo");
        //            return null;
        //        }

        //        var endpoint = $"{apiUrl.TrimEnd('/')}/api/{instanceName}/groups";

        //        var body = new
        //        {
        //            name = name,
        //            participants = validParticipants.Select(x => new { id = x })
        //        };

        //        var response = await HttpClientHelper.SendRequestAsync(
        //            method: Method.POST,
        //            endpoint: endpoint,
        //            data: body,
        //            headers: BuildHeader(apiKey),
        //            cancellationToken: cancellationToken
        //        );

        //        if (response?.IsSuccessful == true && !string.IsNullOrEmpty(response.Content))
        //        {
        //            var result = JsonConvert.DeserializeObject<GroupCreateResult>(response.Content);

        //            _logger.LogInformation(
        //                "Grupo criado com sucesso - Nome: {Name}, JID: {Jid}",
        //                name,
        //                result?.GroupJid
        //            );

        //            return result;
        //        }

        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao criar grupo: {Name}", name);
        //        return null;
        //    }
        //}

        #endregion

        #region Download de Mídia

        /// <summary>
        /// Baixa mídia do WhatsApp para um arquivo local
        /// GET /api/{instance}/files/{keyId}
        /// </summary>
        public async Task<Stream?> DownloadMediaToDiskAsync(
            string destinationFilePath,
            string instanceName,
            string keyId,
            string apiUrl,
            string apiKey,
            CancellationToken cancellationToken = default,
            string? downloadUrl = null)
        {
            try
            {
                var endpoint = downloadUrl ?? $"{apiUrl.TrimEnd('/')}/api/{instanceName}/files/{keyId}";

                _logger.LogInformation(
                    "Baixando mídia - Destino: {Destino}, Endpoint: {Endpoint}",
                    destinationFilePath,
                    endpoint
                );

                using var client = new HttpClient
                {
                    DefaultRequestVersion = HttpVersion.Version11
                };

                using var request = new HttpRequestMessage(HttpMethod.Get, endpoint)
                {
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
                };

                // Adicionar headers se tiver API key
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Add("X-Api-Key", apiKey);
                }

                using var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                ).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
                var destination = new FileStream(
                    destinationFilePath,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    81920,
                    true);

                // Timeout de 15s sem progresso
                using var idleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                idleCts.CancelAfter(TimeSpan.FromSeconds(15));

                var buffer = new byte[81920];
                int read;
                while ((read = await source.ReadAsync(buffer, idleCts.Token)) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, read), idleCts.Token);
                    idleCts.CancelAfter(TimeSpan.FromSeconds(15)); // Reinicia timeout
                }

                // Validar tamanho se houver Content-Length
                if (response.Content.Headers.ContentLength is long len && destination.Length != len)
                {
                    throw new IOException($"Conteúdo incompleto: {destination.Length}/{len} bytes.");
                }

                destination.Position = 0;

                _logger.LogInformation(
                    "Mídia baixada com sucesso - Tamanho: {Size} bytes",
                    destination.Length
                );

                return destination;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao baixar mídia para disco: {Destino}", destinationFilePath);
                return null;
            }
        }

        #endregion

        #region Consulta de Contatos

        /// <summary>
        /// Consulta informações do contato na API do WAHA
        /// GET /api/contacts?contactId={contactId}&session={session}
        /// </summary>
        public async Task<WahaContactInfo?> ObterInformacoesContatoAsync(
            string contactId,
            string session,
            string wahaApiUrl,
            string wahaApiKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validações
                if (string.IsNullOrWhiteSpace(contactId))
                {
                    _logger.LogWarning("ObterInformacoesContatoAsync: ContactId vazio ou nulo");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(session))
                {
                    _logger.LogWarning("ObterInformacoesContatoAsync: Session vazio ou nulo");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(wahaApiUrl))
                {
                    _logger.LogWarning("ObterInformacoesContatoAsync: URL WAHA não configurada");
                    return null;
                }

                // Formatar contactId (apenas números)
                var contactIdLimpo = new string(contactId.Where(char.IsDigit).ToArray());

                // Construir URL
                var url = $"{wahaApiUrl.TrimEnd('/')}/api/contacts?contactId={contactId}&session={session}";

                _logger.LogInformation(
                    "Consultando informações do contato - ContactId: {ContactId}, Session: {Session}",
                    contactIdLimpo,
                    session
                );

                // Configurar headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("accept", "*/*");
                if (!string.IsNullOrWhiteSpace(wahaApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("X-Api-Key", wahaApiKey);
                }

                // Fazer requisição
                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "Erro ao consultar contato - Status: {Status}, Resposta: {Response}",
                        response.StatusCode,
                        errorContent
                    );
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogDebug("Resposta da API WAHA: {Response}", jsonResponse);

                // ✅ CORRIGIDO: API WAHA retorna um único objeto, não um array
                var contact = JsonConvert.DeserializeObject<WahaContactInfo>(jsonResponse);

                if (contact != null)
                {
                    _logger.LogInformation(
                        "Informações do contato obtidas - Nome: {Nome}, PushName: {PushName}, Número: {Numero}",
                        contact.Name ?? "N/A",
                        contact.PushName ?? "N/A",
                        contact.Number ?? "N/A"
                    );

                    return contact;
                }

                _logger.LogWarning("Nenhum contato encontrado para o ID: {ContactId}", contactIdLimpo);
                return null;
            }
                catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao obter informações do contato - ContactId: {ContactId}, Session: {Session}",
                    contactId,
                    session
                );
                return null;
            }
        }

        #endregion

        #region Métodos Auxiliares Privados

        /// <summary>
        /// Handler para validação de número antes do envio
        /// </summary>
        private async Task<(string? chatId, SendMessageResult? error)> HandlerFetchNumberIdAsync(
            string instanceName,
            string phone,
            string apiUrl,
            string apiKey,
            CancellationToken cancellationToken)
        {
            // Se já é um JID válido (grupo ou lid), retornar direto
            if (phone.EndsWith("@g.us") || phone.EndsWith("@lid") || TelefoneHelper.IsJidValido(phone))
            {
                return (phone, null);
            }

            // Validar número
            var result = await FetchNumberIdAsync(instanceName, phone, apiUrl, apiKey, cancellationToken);


            return result.EnumPhoneIdResponse switch
            {
                EnumPhoneIdResponse.registred => (result.NumberId, null),
                EnumPhoneIdResponse.notRegistered => (null, new SendMessageResult(true, EnumSentResult.invalidPhone)),
                EnumPhoneIdResponse.notConnected => (null, new SendMessageResult(true, EnumSentResult.instanceError)),
                _ => (null, new SendMessageResult(false, EnumSentResult.requestError)),
            };
        }

        /// <summary>
        /// Constrói o chatId no formato correto do WAHA
        /// </summary>
        private static string BuildKeyRemoteJid(string keyId)
        {
            // Se já está formatado (grupo ou lid), retorna como está
            if (keyId.EndsWith("@g.us") || keyId.EndsWith("@lid") || TelefoneHelper.IsJidValido(keyId))
            {
                return keyId;
            }

            // Extrair apenas dígitos e adicionar @c.us
            var digitos = TelefoneHelper.ExtrairDigitos(keyId);
            return $"{digitos}@c.us";
        }

        /// <summary>
        /// Remove sufixo _CAPTION do ID de mensagem citada
        /// </summary>
        private static string BuildQuotedMessageId(string quotedMessageId)
        {
            return quotedMessageId.Replace("_CAPTION", "");
        }

        /// <summary>
        /// Constrói headers padrão para requisições WAHA
        /// </summary>
        private Dictionary<string, string> BuildHeader(string apiKey)
        {
            var headers = new Dictionary<string, string>
            {
                { "content-type", "application/json" }
            };

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                headers.Add("X-Api-Key", apiKey);
            }

            return headers;
        }

        private string FormatarNumeroWhatsApp(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                return string.Empty;

            // Se já está formatado, retorna como está
            if (numero.Contains("@"))
                return numero;

            // Remove caracteres não numéricos
            var numeroLimpo = new string(numero.Where(char.IsDigit).ToArray());

            // Adiciona @c.us se não tiver
            if (!string.IsNullOrWhiteSpace(numeroLimpo))
            {
                numeroLimpo = $"{numeroLimpo}@c.us";
            }

            return numeroLimpo;
        }

        private string ObterMimeType(TipoMensagemWhatsApp tipo, string? nomeArquivo = null)
        {
            // Se tiver nome de arquivo, tentar obter mimetype dele
            if (!string.IsNullOrWhiteSpace(nomeArquivo) && TelefoneWhatsHelper.ObterMimeTypeNomeCompleto != null)
            {
                var mimeFromFile = TelefoneWhatsHelper.ObterMimeTypeNomeCompleto(nomeArquivo);
                if (!string.IsNullOrWhiteSpace(mimeFromFile))
                    return mimeFromFile;
            }

            // Fallback para tipo genérico
            return tipo switch
            {
                TipoMensagemWhatsApp.Imagem => "image/jpeg",
                TipoMensagemWhatsApp.Audio => "audio/ogg; codecs=opus",
                TipoMensagemWhatsApp.Video => "video/mp4",
                TipoMensagemWhatsApp.Documento => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        private string ObterEndpointMidia(TipoMensagemWhatsApp tipo)
        {
            return tipo switch
            {
                TipoMensagemWhatsApp.Imagem => "sendImage",
                TipoMensagemWhatsApp.Audio => "sendVoice",
                TipoMensagemWhatsApp.Video => "sendVideo",
                TipoMensagemWhatsApp.Documento => "sendFile",
                _ => "sendText"
            };
        }

        private string ObterExtensao(TipoMensagemWhatsApp tipo)
        {
            return tipo switch
            {
                TipoMensagemWhatsApp.Imagem => "jpg",
                TipoMensagemWhatsApp.Audio => "ogg",
                TipoMensagemWhatsApp.Video => "mp4",
                TipoMensagemWhatsApp.Documento => "pdf",
                _ => "txt"
            };
        }

        private EnvioResponse CriarRespostaErro(string mensagem)
        {
            return new EnvioResponse
            {
                Sucesso = false,
                Mensagem = mensagem,
                DataEnvio = DateTime.UtcNow,
                Erro = mensagem
            };
        }

        #endregion
    }
}