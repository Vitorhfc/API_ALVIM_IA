using Admin_Service.Service.Interface;
using Client_Service.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Classes.Model;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Client_Service.Service
{
    public class WAHAService : IWAHAService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WAHAService> _logger;
        private readonly ILogClientService _logClientService;
        private readonly IEmpresaService _empresaService;
        private readonly string _wahaApiUrl;
        private readonly string? _wahaApiKey;
        private readonly List<string> _webhookUrls;

        public WAHAService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<WAHAService> logger,
            ILogClientService logClientService,
            IEmpresaService empresaService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _logClientService = logClientService;
            _empresaService = empresaService;

            _wahaApiUrl = _configuration["WAHASettings:ApiUrl"]
                ?? throw new InvalidOperationException("Configuração WAHASettings:ApiUrl não encontrada");
            _wahaApiKey = _configuration["WAHASettings:ApiKey"];

            _webhookUrls = _configuration.GetSection("WAHASettings:WebhookUrls")
                .Get<List<string>>() ?? new List<string>();

            _logger.LogInformation("WAHAService inicializado com URL: {Url}, Webhooks: {Count}",
                _wahaApiUrl, _webhookUrls.Count);
        }

        #region Métodos Públicos

        public async Task<WAHAQRCodeResponse> IniciarSessaoAsync(string sessionName)
        {
            try
            {
                _logger.LogInformation("Processo de inicialização de sessão WAHA: {SessionName}", sessionName);

                var httpClient = CriarHttpClient();

                var sessionExiste = await VerificarSessaoExiste(sessionName);

                if (!sessionExiste)
                {
                    await CriarSessaoAsync(sessionName);
                }
                else
                {
                    _logger.LogInformation("Sessão {SessionName} já existe. Pulando criação.", sessionName);
                }

                await IniciarSessaoExistenteAsync(sessionName);

                await Task.Delay(2000);

                var qrCodeResponse = await ObterQRCodeAsync(sessionName);

                await _logClientService.RegistrarInfo(
                    "WAHAService.IniciarSessaoAsync",
                    $"Sessão WAHA iniciada com sucesso: {sessionName}"
                );

                return qrCodeResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar sessão WAHA: {SessionName}", sessionName);
                await _logClientService.RegistrarErro(
                    "WAHAService.IniciarSessaoAsync",
                    ex.Message,
                    sessionName,
                    ex.StackTrace
                );
                throw;
            }
        }

        private async Task<bool> VerificarSessaoExiste(string sessionName)
        {
            try
            {
                var httpClient = CriarHttpClient();
                var response = await httpClient.GetAsync($"{_wahaApiUrl}/api/sessions/{sessionName}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task CriarSessaoAsync(string sessionName)
        {
            _logger.LogInformation("Criando nova sessão WAHA: {SessionName}", sessionName);

            var httpClient = CriarHttpClient();

            var empresaId = ExtrairEmpresaIdDoNomeSessao(sessionName);

            var request = new WAHAIniciarSessaoRequest
            {
                Name = sessionName,
                Start = true,
                Config = CriarConfiguracaoPadrao(empresaId)
            };

            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{_wahaApiUrl}/api/sessions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                _logger.LogWarning(
                    "Erro 422 ao criar sessão. Tentando fazer restart primeiro. Sessão: {SessionName}",
                    sessionName
                );

                await RestartSessaoInternoAsync(sessionName);

                await Task.Delay(1000);

                jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                response = await httpClient.PostAsync($"{_wahaApiUrl}/api/sessions", content);
                responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Erro ao criar sessão após restart. Status: {StatusCode}, Resposta: {Response}",
                        response.StatusCode,
                        responseContent
                    );
                    throw new HttpRequestException($"Erro ao criar sessão após restart: {response.StatusCode} - {responseContent}");
                }

                _logger.LogInformation("Sessão WAHA criada com sucesso após restart: {SessionName}", sessionName);

                // Atualizar sessão no banco após criação bem-sucedida
                await AtualizarSessionNoBancoAsync(empresaId, sessionName);
            }
            else if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Erro ao criar sessão WAHA. Status: {StatusCode}, Resposta: {Response}",
                    response.StatusCode,
                    responseContent
                );
                throw new HttpRequestException($"Erro ao criar sessão: {response.StatusCode} - {responseContent}");
            }
            else
            {
                _logger.LogInformation("Sessão WAHA criada com sucesso: {SessionName}", sessionName);

                // Atualizar sessão no banco após criação bem-sucedida
                await AtualizarSessionNoBancoAsync(empresaId, sessionName);
            }
        }

        private async Task IniciarSessaoExistenteAsync(string sessionName)
        {
            _logger.LogInformation("Iniciando sessão existente: {SessionName}", sessionName);

            var httpClient = CriarHttpClient();

            var response = await httpClient.PostAsync($"{_wahaApiUrl}/api/sessions/{sessionName}/start", null);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                _logger.LogWarning(
                    "Erro 422 ao iniciar sessão. Tentando fazer restart primeiro. Sessão: {SessionName}",
                    sessionName
                );

                await RestartSessaoInternoAsync(sessionName);

                await Task.Delay(1000);

                response = await httpClient.PostAsync($"{_wahaApiUrl}/api/sessions/{sessionName}/start", null);
                responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Erro ao iniciar sessão após restart. Status: {StatusCode}, Resposta: {Response}",
                        response.StatusCode,
                        responseContent
                    );
                }
                else
                {
                    _logger.LogInformation("Sessão iniciada com sucesso após restart: {SessionName}", sessionName);
                }
            }
            else if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Erro ao iniciar sessão (pode já estar iniciada). Status: {StatusCode}, Resposta: {Response}",
                    response.StatusCode,
                    responseContent
                );
            }
            else
            {
                _logger.LogInformation("Sessão iniciada com sucesso: {SessionName}", sessionName);
            }
        }

        private async Task RestartSessaoInternoAsync(string sessionName)
        {
            try
            {
                _logger.LogInformation("Fazendo restart da sessão: {SessionName}", sessionName);

                var httpClient = CriarHttpClient();
                var response = await httpClient.PostAsync($"{_wahaApiUrl}/api/sessions/{sessionName}/restart", null);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Restart da sessão executado com sucesso: {SessionName}", sessionName);
                }
                else
                {
                    _logger.LogWarning(
                        "Erro ao fazer restart. Status: {StatusCode}, Resposta: {Response}",
                        response.StatusCode,
                        responseContent
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exceção ao fazer restart da sessão: {SessionName}", sessionName);
            }
        }

        public async Task<WAHAQRCodeResponse> ObterQRCodeAsync(string sessionName)
        {
            try
            {
                _logger.LogInformation("Obtendo QR Code da sessão: {SessionName}", sessionName);

                var httpClient = CriarHttpClient();

                var response = await httpClient.GetAsync($"{_wahaApiUrl}/api/{sessionName}/auth/qr?format=raw");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation(
                        "Sessão não encontrada ao obter QR Code: {SessionName}",
                        sessionName
                    );

                    return new WAHAQRCodeResponse
                    {
                        State = WAHAStatusEnum.STOPPED,
                        Message = "Sessão não encontrada. Inicie uma nova sessão primeiro.",
                        Sucesso = false,
                        Erro = "Sessão não existe"
                    };
                }

                if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                {
                    _logger.LogWarning(
                        "Erro 422 ao obter QR Code. Tentando fazer restart primeiro. Sessão: {SessionName}",
                        sessionName
                    );

                    await RestartSessaoInternoAsync(sessionName);

                    await Task.Delay(2000);

                    response = await httpClient.GetAsync($"{_wahaApiUrl}/api/{sessionName}/auth/qr?format=raw");
                    responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning(
                            "Erro ao obter QR Code após restart. Status: {StatusCode}, Resposta: {Response}",
                            response.StatusCode,
                            responseContent
                        );

                        var status = await ObterStatusSessaoAsync(sessionName);
                        return new WAHAQRCodeResponse
                        {
                            State = status.Status,
                            Message = status.EstaConectado ? "Sessão já está conectada" : "QR Code não disponível após restart"
                        };
                    }

                    _logger.LogInformation("QR Code obtido com sucesso após restart: {SessionName}", sessionName);
                }
                else if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Erro ao obter QR Code. Status: {StatusCode}, Resposta: {Response}",
                        response.StatusCode,
                        responseContent
                    );

                    var status = await ObterStatusSessaoAsync(sessionName);
                    return new WAHAQRCodeResponse
                    {
                        State = status.Status,
                        Message = status.EstaConectado ? "Sessão já está conectada" : "QR Code não disponível"
                    };
                }

                var qrData = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (qrData != null && qrData.ContainsKey("value"))
                {
                    var qrValue = qrData["value"];
                    _logger.LogInformation("QR Code obtido com sucesso para sessão: {SessionName}", sessionName);

                    return new WAHAQRCodeResponse
                    {
                        QRCode = qrValue,
                        State = WAHAStatusEnum.SCAN_QR_CODE,
                        Message = "QR Code gerado com sucesso",
                        Sucesso = true
                    };
                }

                return new WAHAQRCodeResponse
                {
                    State = "UNKNOWN",
                    Message = "Resposta inválida do servidor WAHA"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter QR Code da sessão: {SessionName}", sessionName);
                await _logClientService.RegistrarErro(
                    "WAHAService.ObterQRCodeAsync",
                    ex.Message,
                    sessionName,
                    ex.StackTrace
                );
                throw;
            }
        }

        public async Task<WAHAStatusResponse> ObterStatusSessaoAsync(string sessionName)
        {
            try
            {
                _logger.LogInformation("Obtendo status da sessão: {SessionName}", sessionName);

                var httpClient = CriarHttpClient();
                var response = await httpClient.GetAsync($"{_wahaApiUrl}/api/sessions/{sessionName}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation(
                        "Sessão não encontrada: {SessionName}. Retornando status STOPPED",
                        sessionName
                    );

                    return new WAHAStatusResponse
                    {
                        Name = sessionName,
                        Status = WAHAStatusEnum.STOPPED,
                        State = WAHAStatusEnum.STOPPED,
                        Mensagem = "Sessão não encontrada",
                        Sucesso = true
                    };
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Erro ao obter status. Status: {StatusCode}, Resposta: {Response}",
                        response.StatusCode,
                        responseContent
                    );
                    throw new HttpRequestException($"Erro ao obter status da sessão: {response.StatusCode}");
                }

                var statusResponse = JsonSerializer.Deserialize<WAHAStatusResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (statusResponse != null)
                {
                    _logger.LogInformation(
                        "Status obtido - Sessão: {SessionName}, Status: {Status}",
                        sessionName,
                        statusResponse.Status
                    );
                }

                return statusResponse ?? new WAHAStatusResponse
                {
                    Name = sessionName,
                    Status = WAHAStatusEnum.STOPPED
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter status da sessão: {SessionName}", sessionName);
                await _logClientService.RegistrarErro(
                    "WAHAService.ObterStatusSessaoAsync",
                    ex.Message,
                    sessionName,
                    ex.StackTrace
                );
                throw;
            }
        }

        public async Task<WAHAActionResponse> PararSessaoAsync(string sessionName)
        {
            try
            {
                _logger.LogInformation("Parando sessão WAHA: {SessionName}", sessionName);

                var httpClient = CriarHttpClient();
                var response = await httpClient.PostAsync($"{_wahaApiUrl}/api/sessions/{sessionName}/stop", null);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Erro ao parar sessão. Status: {StatusCode}, Resposta: {Response}",
                        response.StatusCode,
                        responseContent
                    );
                    return new WAHAActionResponse
                    {
                        Success = false,
                        Error = $"Erro ao parar sessão: {response.StatusCode}"
                    };
                }

                await _logClientService.RegistrarInfo(
                    "WAHAService.PararSessaoAsync",
                    $"Sessão WAHA parada com sucesso: {sessionName}"
                );

                return new WAHAActionResponse
                {
                    Success = true,
                    Message = "Sessão parada com sucesso"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao parar sessão WAHA: {SessionName}", sessionName);
                await _logClientService.RegistrarErro(
                    "WAHAService.PararSessaoAsync",
                    ex.Message,
                    sessionName,
                    ex.StackTrace
                );
                throw;
            }
        }

        public async Task<WAHAActionResponse> RemoverSessaoAsync(string sessionName)
        {
            try
            {
                _logger.LogInformation("Removendo sessão WAHA: {SessionName}", sessionName);

                var httpClient = CriarHttpClient();
                var response = await httpClient.DeleteAsync($"{_wahaApiUrl}/api/sessions/{sessionName}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Erro ao remover sessão. Status: {StatusCode}, Resposta: {Response}",
                        response.StatusCode,
                        responseContent
                    );
                    return new WAHAActionResponse
                    {
                        Success = false,
                        Error = $"Erro ao remover sessão: {response.StatusCode}"
                    };
                }

                await _logClientService.RegistrarInfo(
                    "WAHAService.RemoverSessaoAsync",
                    $"Sessão WAHA removida com sucesso: {sessionName}"
                );

                return new WAHAActionResponse
                {
                    Success = true,
                    Message = "Sessão removida com sucesso"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover sessão WAHA: {SessionName}", sessionName);
                await _logClientService.RegistrarErro(
                    "WAHAService.RemoverSessaoAsync",
                    ex.Message,
                    sessionName,
                    ex.StackTrace
                );
                throw;
            }
        }

        public async Task<List<WAHASessionInfo>> ListarSessoesAsync()
        {
            try
            {
                _logger.LogInformation("Listando todas as sessões WAHA");

                var httpClient = CriarHttpClient();
                var response = await httpClient.GetAsync($"{_wahaApiUrl}/api/sessions");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Erro ao listar sessões. Status: {StatusCode}, Resposta: {Response}",
                        response.StatusCode,
                        responseContent
                    );
                    throw new HttpRequestException($"Erro ao listar sessões: {response.StatusCode}");
                }

                var sessions = JsonSerializer.Deserialize<List<WAHASessionInfo>>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Total de sessões encontradas: {Count}", sessions?.Count ?? 0);

                return sessions ?? new List<WAHASessionInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar sessões WAHA");
                await _logClientService.RegistrarErro(
                    "WAHAService.ListarSessoesAsync",
                    ex.Message,
                    null,
                    ex.StackTrace
                );
                throw;
            }
        }

        public async Task<WAHAActionResponse> ReiniciarSessaoAsync(string sessionName)
        {
            try
            {
                _logger.LogInformation("Reiniciando sessão WAHA: {SessionName}", sessionName);

                var httpClient = CriarHttpClient();
                var response = await httpClient.PostAsync($"{_wahaApiUrl}/api/sessions/{sessionName}/restart", null);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Erro ao reiniciar sessão. Status: {StatusCode}, Resposta: {Response}",
                        response.StatusCode,
                        responseContent
                    );
                    return new WAHAActionResponse
                    {
                        Success = false,
                        Error = $"Erro ao reiniciar sessão: {response.StatusCode}"
                    };
                }

                await _logClientService.RegistrarInfo(
                    "WAHAService.ReiniciarSessaoAsync",
                    $"Sessão WAHA reiniciada com sucesso: {sessionName}"
                );

                return new WAHAActionResponse
                {
                    Success = true,
                    Message = "Sessão reiniciada com sucesso"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reiniciar sessão WAHA: {SessionName}", sessionName);
                await _logClientService.RegistrarErro(
                    "WAHAService.ReiniciarSessaoAsync",
                    ex.Message,
                    sessionName,
                    ex.StackTrace
                );
                throw;
            }
        }

        public async Task<WAHAAccountInfo> ObterInformacoesContaAsync(string sessionName)
        {
            try
            {
                _logger.LogInformation("Obtendo informações da conta da sessão: {SessionName}", sessionName);

                var httpClient = CriarHttpClient();
                var response = await httpClient.GetAsync($"{_wahaApiUrl}/api/{sessionName}/contacts/me");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Erro ao obter informações da conta. Status: {StatusCode}, Resposta: {Response}",
                        response.StatusCode,
                        responseContent
                    );
                    throw new HttpRequestException($"Erro ao obter informações da conta: {response.StatusCode}");
                }

                var accountInfo = JsonSerializer.Deserialize<WAHAAccountInfo>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (accountInfo != null)
                {
                    _logger.LogInformation(
                        "Informações da conta obtidas - Sessão: {SessionName}, ID: {Id}",
                        sessionName,
                        accountInfo.Id
                    );
                }

                return accountInfo ?? new WAHAAccountInfo { Id = "UNKNOWN" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter informações da conta: {SessionName}", sessionName);
                await _logClientService.RegistrarErro(
                    "WAHAService.ObterInformacoesContaAsync",
                    ex.Message,
                    sessionName,
                    ex.StackTrace
                );
                throw;
            }
        }

        #endregion

        #region Métodos Auxiliares

        private HttpClient CriarHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            if (!string.IsNullOrEmpty(_wahaApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-Api-Key", _wahaApiKey);
            }

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }

        public string GerarNomeSessao(string empresaId)
        {
            var dataAtual = DateTime.Now.ToString("ddMMyyyy");
            var nomeSessao = $"Alvim_{empresaId}_{dataAtual}";
            _logger.LogInformation("Nome de sessão gerado: {NomeSessao} para empresa: {EmpresaId}", nomeSessao, empresaId);
            return nomeSessao;
        }

        public async Task<string> GerarESalvarNomeSessaoAsync(string empresaId)
        {
            try
            {
                var empresa = await _empresaService.BuscarPorIdAsync(empresaId);

                if (empresa == null)
                {
                    _logger.LogError("Empresa não encontrada ao tentar salvar nome de sessão: {EmpresaId}", empresaId);
                    throw new InvalidOperationException($"Empresa com ID {empresaId} não encontrada");
                }

                if (!string.IsNullOrWhiteSpace(empresa.WahaSessionName))
                {
                    _logger.LogInformation(
                        "Empresa já possui nome de sessão configurado: {NomeSessao} para empresa: {EmpresaId}",
                        empresa.WahaSessionName,
                        empresaId
                    );

                    await _logClientService.RegistrarInfo(
                        "WAHAService.GerarESalvarNomeSessaoAsync",
                        $"Empresa já possui nome de sessão: {empresa.WahaSessionName} para empresa: {empresaId}"
                    );

                    return empresa.WahaSessionName;
                }

                var nomeSessao = GerarNomeSessao(empresaId);

                empresa.WahaSessionName = nomeSessao;
                empresa.DtaAlteracao = DateTime.Now;

                await _empresaService.EditarAsync(empresa);

                _logger.LogInformation(
                    "Nome de sessão gerado e salvo no banco: {NomeSessao} para empresa: {EmpresaId}",
                    nomeSessao,
                    empresaId
                );

                await _logClientService.RegistrarInfo(
                    "WAHAService.GerarESalvarNomeSessaoAsync",
                    $"Nome de sessão gerado e salvo: {nomeSessao} para empresa: {empresaId}"
                );

                return nomeSessao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar e salvar nome de sessão para empresa: {EmpresaId}", empresaId);
                await _logClientService.RegistrarErro(
                    "WAHAService.GerarESalvarNomeSessaoAsync",
                    ex.Message,
                    empresaId,
                    ex.StackTrace ?? string.Empty
                );
                throw;
            }
        }

        private async Task AtualizarSessionNoBancoAsync(string empresaId, string sessionName)
        {
            try
            {
                var empresa = await _empresaService.BuscarPorIdAsync(empresaId);

                if (empresa == null)
                {
                    _logger.LogWarning("Empresa não encontrada ao tentar atualizar session name: {EmpresaId}", empresaId);
                    return;
                }

                // Só atualizar se o valor mudou
                if (empresa.WahaSessionName != sessionName)
                {
                    empresa.WahaSessionName = sessionName;
                    empresa.DtaAlteracao = DateTime.Now;

                    await _empresaService.EditarAsync(empresa);

                    _logger.LogInformation(
                        "Session name atualizado no banco: {SessionName} para empresa: {EmpresaId}",
                        sessionName,
                        empresaId
                    );

                    await _logClientService.RegistrarInfo(
                        "WAHAService.AtualizarSessionNoBancoAsync",
                        $"Session name atualizado: {sessionName} para empresa: {empresaId}"
                    );
                }
                else
                {
                    _logger.LogInformation(
                        "Session name já está correto no banco: {SessionName} para empresa: {EmpresaId}",
                        sessionName,
                        empresaId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar session name no banco para empresa: {EmpresaId}", empresaId);
                await _logClientService.RegistrarErro(
                    "WAHAService.AtualizarSessionNoBancoAsync",
                    ex.Message,
                    empresaId,
                    ex.StackTrace ?? string.Empty
                );
                // Não propagar a exceção para não quebrar o fluxo principal
                // O importante é que a sessão WAHA foi criada com sucesso
            }
        }

        private string ExtrairEmpresaIdDoNomeSessao(string sessionName)
        {
            try
            {
                var partes = sessionName.Split('_');

                if (partes.Length >= 3)
                {
                    var empresaId = partes[1];
                    _logger.LogInformation("EmpresaId extraído: {EmpresaId} do sessionName: {SessionName}", empresaId, sessionName);
                    return empresaId;
                }

                _logger.LogWarning("Formato de sessionName inválido: {SessionName}. Esperado: Alvim_{{empresaId}}_{{data}}", sessionName);
                throw new ArgumentException($"SessionName em formato inválido: {sessionName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao extrair empresaId do sessionName: {SessionName}", sessionName);
                throw;
            }
        }

        private WAHASessionConfig CriarConfiguracaoPadrao(string empresaId)
        {
            var webhooks = _webhookUrls.Select(url => new WAHAWebhookConfig
            {
                Url = $"{url}/{empresaId}",
                Events = new List<string> { "session.status", "message" },
                Hmac = new WAHAHmacConfig { Key = null },
                Retries = new WAHARetriesConfig
                {
                    DelaySeconds = 1,
                    Attempts = 1,
                    Policy = "constant"
                },
                CustomHeaders = null
            }).ToList();

            _logger.LogInformation("Webhooks configurados para empresaId {EmpresaId}: {Urls}",
                empresaId,
                string.Join(", ", webhooks.Select(w => w.Url)));

            return new WAHASessionConfig
            {
                Metadata = new Dictionary<string, object>(),
                Proxy = null,
                Debug = false,
                Ignore = new WAHAIgnoreConfig
                {
                    Status = false,
                    Groups = false,
                    Channels = false,
                    Broadcast = true
                },
                Noweb = new WAHANowebConfig
                {
                    MarkOnline = true,
                    Store = new WAHAStoreConfig
                    {
                        Enabled = true,
                        FullSync = false
                    }
                },
                Webjs = new WAHAWebjsConfig
                {
                    TagsEventsOn = false
                },
                Webhooks = webhooks
            };
        }

        #endregion
    }
}