using Admin_Repository.Repositorio.Interface;
using Admin_Service.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Classes.Entidades.ADM;
using Shared.Classes.Model;
using Shared.Services.Interface;
using Shared.Utils.Criptografia;

namespace Admin_Service.Service
{
    /// <summary>
    /// Serviço para provisionamento automático de empresas (instância WAHA, database, etc)
    /// </summary>
    public class EmpresaProvisionamentoService : IEmpresaProvisionamentoService
    {
        #region Campos

        private readonly IEmpresaRepository _empresaRepository;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmpresaProvisionamentoService> _logger;

        #endregion

        #region Construtor

        public EmpresaProvisionamentoService(
            IEmpresaRepository empresaRepository,
            IWhatsAppService whatsAppService,
            IConfiguration configuration,
            ILogger<EmpresaProvisionamentoService> logger)
        {
            _empresaRepository = empresaRepository;
            _whatsAppService = whatsAppService;
            _configuration = configuration;
            _logger = logger;
        }

        #endregion

        #region Métodos Públicos

        public async Task<ResultadoProvisionamento> ProvisionarEmpresaAsync(Empresa empresa)
        {
            var resultado = new ResultadoProvisionamento();

            try
            {
                _logger.LogInformation(
                    "Iniciando provisionamento da empresa {EmpresaId} - {RazaoSocial}",
                    empresa.Id,
                    empresa.RazaoSocial
                );

                resultado.Logs.Add($"Iniciando provisionamento da empresa: {empresa.RazaoSocial}");

                // Obter configurações globais WAHA
                var wahaApiUrl = _configuration["WAHASettings:ApiUrl"];
                var wahaApiKey = _configuration["WAHASettings:ApiKey"];

                if (string.IsNullOrWhiteSpace(wahaApiUrl) || string.IsNullOrWhiteSpace(wahaApiKey))
                {
                    resultado.Mensagem = "Configuração global WAHA não encontrada no appsettings";
                    _logger.LogError(resultado.Mensagem);
                    return resultado;
                }

                // Validar se empresa tem SessionName
                if (string.IsNullOrWhiteSpace(empresa.WahaSessionName))
                {
                    resultado.Mensagem = "Nome da sessão WAHA não foi definido para esta empresa";
                    _logger.LogWarning(resultado.Mensagem);
                    return resultado;
                }

                resultado.Logs.Add($"Usando configurações WAHA globais - Session: {empresa.WahaSessionName}");

                // 1. Criar sessão no WAHA com webhook já configurado
                _logger.LogInformation("Criando sessão WAHA: {SessionName}", empresa.WahaSessionName);
                resultado.Logs.Add($"Criando sessão WAHA: {empresa.WahaSessionName}");

                // Obter URL do webhook
                var webhookUrl = ObterUrlWebhook();
                resultado.Logs.Add($"Webhook URL: {webhookUrl}");

                // Criar request com webhook já configurado
                var sessionRequest = new CreateWahaSessionRequest
                {
                    name = empresa.WahaSessionName,
                    start = true,
                    config = new WahaSessionConfig
                    {
                        metadata = new Dictionary<string, string>
                        {
                            { "empresa.id", empresa.Id },
                            { "empresa.razaoSocial", empresa.RazaoSocial },
                            { "empresa.numeroWhatsApp", empresa.WahaNumeroWhatsApp ?? "" }
                        },
                        webhooks = new List<WahaWebhookConfig>
                        {
                            new WahaWebhookConfig
                            {
                                url = webhookUrl,
                                events = new List<string> { "message", "message.reaction" },
                                hmac = null,
                                retries = 3,
                                customHeaders = null
                            }
                        }
                    }
                };

                var createResult = await _whatsAppService.CreateSessionAsync(
                    sessionRequest,
                    wahaApiUrl,
                    wahaApiKey
                );

                if (createResult == null)
                {
                    resultado.Mensagem = "Falha ao criar sessão WAHA";
                    _logger.LogError(resultado.Mensagem);
                    resultado.Logs.Add($"ERRO: {resultado.Mensagem}");
                    return resultado;
                }

                resultado.InstanciaWahaCriada = true;
                resultado.WebhookConfigurado = true;
                resultado.Logs.Add($"Sessão WAHA criada com sucesso - Status: {createResult.status}");
                resultado.Logs.Add("Webhook configurado automaticamente na criação da sessão");
                _logger.LogInformation("Sessão WAHA criada com sucesso: {SessionName}", empresa.WahaSessionName);

                // Aguardar um pouco para a instância inicializar
                await Task.Delay(2000);

                // 3. Atualizar empresa com flag ativo
                empresa.FlgWahaAtivo = true;
                empresa.WahaDataConexao = null; // Ainda não está conectado
                empresa.WahaUltimaVerificacao = DateTime.Now;

                await _empresaRepository.EditarAsync(empresa);
                resultado.Logs.Add("Empresa atualizada com status de provisionamento");

                // 4. Sucesso final
                resultado.Sucesso = true;
                resultado.Mensagem = "Empresa provisionada com sucesso. Agora é necessário escanear o QR Code para conectar";
                resultado.Logs.Add("Provisionamento concluído com sucesso");

                _logger.LogInformation(
                    "Provisionamento concluído com sucesso para empresa {EmpresaId}",
                    empresa.Id
                );

                return resultado;
            }
            catch (Exception ex)
            {
                resultado.Sucesso = false;
                resultado.Mensagem = "Erro ao provisionar empresa";
                resultado.Erro = ex.Message;
                resultado.Logs.Add($"ERRO FATAL: {ex.Message}");

                _logger.LogError(
                    ex,
                    "Erro ao provisionar empresa {EmpresaId}",
                    empresa.Id
                );

                return resultado;
            }
        }

        public async Task<bool> ReconectarInstanciaAsync(string empresaId)
        {
            try
            {
                var empresa = await _empresaRepository.BuscarPorIdAsync(empresaId);

                if (empresa == null)
                {
                    _logger.LogWarning("Empresa não encontrada: {EmpresaId}", empresaId);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(empresa.WahaSessionName))
                {
                    _logger.LogWarning("Empresa sem SessionName configurada: {EmpresaId}", empresaId);
                    return false;
                }

                // Obter configurações globais
                var wahaApiUrl = _configuration["WAHASettings:ApiUrl"];
                var wahaApiKey = _configuration["WAHASettings:ApiKey"];

                if (string.IsNullOrWhiteSpace(wahaApiUrl) || string.IsNullOrWhiteSpace(wahaApiKey))
                {
                    _logger.LogError("Configuração global WAHA não encontrada");
                    return false;
                }

                _logger.LogInformation(
                    "Reiniciando sessão WAHA para empresa {EmpresaId} - Session: {SessionName}",
                    empresaId,
                    empresa.WahaSessionName
                );

                var result = await _whatsAppService.RestartSessionAsync(
                    empresa.WahaSessionName,
                    wahaApiUrl,
                    wahaApiKey
                );

                if (result)
                {
                    empresa.WahaUltimaVerificacao = DateTime.Now;
                    await _empresaRepository.EditarAsync(empresa);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reconectar instância da empresa: {EmpresaId}", empresaId);
                return false;
            }
        }

        public async Task<StatusConexao> VerificarStatusConexaoAsync(string empresaId)
        {
            try
            {
                var empresa = await _empresaRepository.BuscarPorIdAsync(empresaId);

                if (empresa == null)
                {
                    return new StatusConexao
                    {
                        Conectado = false,
                        Status = "ERROR",
                        Mensagem = "Empresa não encontrada"
                    };
                }

                if (string.IsNullOrWhiteSpace(empresa.WahaSessionName))
                {
                    return new StatusConexao
                    {
                        Conectado = false,
                        Status = "NOT_CONFIGURED",
                        Mensagem = "Empresa sem SessionName configurada"
                    };
                }

                // Obter configurações globais
                var wahaApiUrl = _configuration["WAHASettings:ApiUrl"];
                var wahaApiKey = _configuration["WAHASettings:ApiKey"];

                if (string.IsNullOrWhiteSpace(wahaApiUrl) || string.IsNullOrWhiteSpace(wahaApiKey))
                {
                    return new StatusConexao
                    {
                        Conectado = false,
                        Status = "ERROR",
                        Mensagem = "Configuração global WAHA não encontrada"
                    };
                }

                var statusResponse = await _whatsAppService.GetInstanceStatusAsync(
                    empresa.WahaSessionName,
                    wahaApiUrl,
                    wahaApiKey
                );

                // Atualizar empresa com status atual
                if (statusResponse.Sucesso)
                {
                    var conectadoAntes = empresa.WahaDataConexao != null;
                    var conectadoAgora = statusResponse.IsConnected;

                    // Se mudou de desconectado para conectado, registrar data de conexão
                    if (!conectadoAntes && conectadoAgora)
                    {
                        empresa.WahaDataConexao = DateTime.Now;
                        _logger.LogInformation(
                            "Empresa {EmpresaId} conectou WhatsApp pela primeira vez",
                            empresaId
                        );
                    }
                    // Se mudou de conectado para desconectado, limpar data de conexão
                    else if (conectadoAntes && !conectadoAgora)
                    {
                        empresa.WahaDataConexao = null;
                        _logger.LogWarning(
                            "Empresa {EmpresaId} desconectou WhatsApp",
                            empresaId
                        );
                    }

                    empresa.WahaUltimaVerificacao = DateTime.Now;
                    await _empresaRepository.EditarAsync(empresa);
                }

                return new StatusConexao
                {
                    Conectado = statusResponse.IsConnected,
                    Status = statusResponse.Status,
                    Mensagem = statusResponse.Mensagem,
                    UltimaVerificacao = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar status de conexão da empresa: {EmpresaId}", empresaId);

                return new StatusConexao
                {
                    Conectado = false,
                    Status = "ERROR",
                    Mensagem = $"Erro ao verificar status: {ex.Message}"
                };
            }
        }

        public async Task<ConfigurarWahaResponse> ConfigurarOuReconfigurarWahaAsync(
            string empresaId,
            ConfigurarWahaRequest request)
        {
            var response = new ConfigurarWahaResponse
            {
                Acoes = new AcoesRealizadas(),
                Logs = new List<string>()
            };

            try
            {
                _logger.LogInformation(
                    "Iniciando configuração/reconfiguração WAHA para empresa {EmpresaId}",
                    empresaId
                );
                response.Logs.Add($"Iniciando configuração WAHA para empresa {empresaId}");

                // 1. Buscar empresa
                var empresa = await _empresaRepository.BuscarPorIdAsync(empresaId);

                if (empresa == null)
                {
                    response.Mensagem = "Empresa não encontrada";
                    return response;
                }

                response.Logs.Add($"Empresa encontrada: {empresa.RazaoSocial}");

                // 2. Obter configurações globais WAHA
                var wahaApiUrl = _configuration["WAHASettings:ApiUrl"];
                var wahaApiKey = _configuration["WAHASettings:ApiKey"];

                if (string.IsNullOrWhiteSpace(wahaApiUrl) || string.IsNullOrWhiteSpace(wahaApiKey))
                {
                    response.Mensagem = "Configuração global WAHA não encontrada no appsettings";
                    _logger.LogError(response.Mensagem);
                    return response;
                }

                // 3. Guardar configuração anterior
                response.ConfiguracaoAnterior = new ConfiguracaoWahaInfo
                {
                    NumeroWhatsApp = empresa.WahaNumeroWhatsApp,
                    SessionName = empresa.WahaSessionName,
                    FlgAtivo = empresa.FlgWahaAtivo,
                    DataConexao = empresa.WahaDataConexao
                };

                // 4. Determinar se precisa criar ou reconfigurar
                var temConfiguracao = !string.IsNullOrWhiteSpace(empresa.WahaSessionName);
                var numeroMudou = empresa.WahaNumeroWhatsApp != request.NumeroWhatsApp;
                var sessionMudou = empresa.WahaSessionName != request.WahaSessionName;

                _logger.LogInformation(
                    "Status: TemConfiguracao={TemConfiguracao}, NumeroMudou={NumeroMudou}, SessionMudou={SessionMudou}",
                    temConfiguracao,
                    numeroMudou,
                    sessionMudou
                );

                // 5. Atualizar dados da empresa
                empresa.WahaSessionName = request.WahaSessionName;
                empresa.WahaNumeroWhatsApp = request.NumeroWhatsApp;

                if (numeroMudou)
                {
                    response.Acoes.NumeroAlterado = true;
                    response.Logs.Add($"Número alterado de '{response.ConfiguracaoAnterior?.NumeroWhatsApp}' para '{request.NumeroWhatsApp}'");

                    // Se mudou o número, limpar data de conexão
                    empresa.WahaDataConexao = null;
                }

                // 6. Criar ou reconfigurar sessão no WAHA
                if (!temConfiguracao || sessionMudou || numeroMudou)
                {
                    response.Logs.Add($"Criando nova sessão WAHA: {request.WahaSessionName}");

                    // Obter URL do webhook
                    var webhookUrl = ObterUrlWebhook();
                    response.Logs.Add($"Webhook URL: {webhookUrl}");

                    // Criar request com webhook já configurado
                    var sessionRequest = new CreateWahaSessionRequest
                    {
                        name = request.WahaSessionName,
                        start = true,
                        config = new WahaSessionConfig
                        {
                            metadata = new Dictionary<string, string>
                            {
                                { "empresa.id", empresaId },
                                { "empresa.razaoSocial", empresa.RazaoSocial },
                                { "empresa.numeroWhatsApp", request.NumeroWhatsApp }
                            },
                            webhooks = new List<WahaWebhookConfig>
                            {
                                new WahaWebhookConfig
                                {
                                    url = webhookUrl,
                                    events = new List<string> { "message", "message.reaction" },
                                    hmac = null,
                                    retries = 3,
                                    customHeaders = null
                                }
                            }
                        }
                    };

                    var createResult = await _whatsAppService.CreateSessionAsync(
                        sessionRequest,
                        wahaApiUrl,
                        wahaApiKey
                    );

                    if (createResult == null)
                    {
                        response.Sucesso = false;
                        response.Mensagem = "Falha ao criar sessão WAHA";
                        response.Logs.Add($"ERRO: {response.Mensagem}");
                        return response;
                    }

                    response.Acoes.InstanciaCriada = true;
                    response.Acoes.WebhookConfigurado = true;
                    response.Logs.Add($"Sessão WAHA criada com sucesso - Status: {createResult.status}");
                    response.Logs.Add("Webhook configurado automaticamente na criação da sessão");

                    // Aguardar inicialização
                    await Task.Delay(2000);
                }
                else
                {
                    response.Acoes.InstanciaReconfigurada = true;
                    response.Logs.Add("Sessão WAHA existente mantida");

                    // Configurar webhook separadamente para sessões existentes
                    var webhookUrl = ObterUrlWebhook();
                    response.Logs.Add($"Configurando webhook: {webhookUrl}");

                    var webhookConfigured = await _whatsAppService.SetWebhookAsync(
                        request.WahaSessionName,
                        webhookUrl,
                        wahaApiUrl,
                        wahaApiKey
                    );

                    if (webhookConfigured)
                    {
                        response.Acoes.WebhookConfigurado = true;
                        response.Logs.Add("Webhook configurado com sucesso");
                    }
                    else
                    {
                        response.Logs.Add("AVISO: Falha ao configurar webhook");
                    }
                }

                // 8. Ativar sessão se estava desativada
                if (empresa.FlgWahaAtivo != true)
                {
                    empresa.FlgWahaAtivo = true;
                    response.Acoes.InstanciaAtivada = true;
                    response.Logs.Add("Sessão WAHA ativada");
                }

                // 9. Atualizar última verificação
                empresa.WahaUltimaVerificacao = DateTime.Now;

                // 10. Salvar empresa
                await _empresaRepository.EditarAsync(empresa);
                response.Logs.Add("Empresa atualizada no banco de dados");

                // 11. Configuração atual
                response.ConfiguracaoAtual = new ConfiguracaoWahaInfo
                {
                    NumeroWhatsApp = empresa.WahaNumeroWhatsApp,
                    SessionName = empresa.WahaSessionName,
                    FlgAtivo = empresa.FlgWahaAtivo,
                    DataConexao = empresa.WahaDataConexao
                };

                // 13. Sucesso
                response.Sucesso = true;
                response.Mensagem = "Configuração WAHA atualizada com sucesso. Escaneie o QR Code para conectar.";
                response.Logs.Add("Configuração concluída com sucesso");

                _logger.LogInformation(
                    "Configuração WAHA concluída para empresa {EmpresaId}",
                    empresaId
                );

                return response;
            }
            catch (Exception ex)
            {
                response.Sucesso = false;
                response.Mensagem = $"Erro ao configurar WAHA: {ex.Message}";
                response.Logs.Add($"ERRO FATAL: {ex.Message}");

                _logger.LogError(
                    ex,
                    "Erro ao configurar WAHA para empresa {EmpresaId}",
                    empresaId
                );

                return response;
            }
        }

        #endregion

        #region Métodos Privados

        private string ObterUrlWebhook()
        {
            // Obter URL base da API do Client a partir da configuração
            var clientApiUrl = _configuration["ClientApiBaseUrl"] ??
                              _configuration["ApiBaseUrl"] ??
                              "https://localhost:7002"; // Fallback

            return $"{clientApiUrl.TrimEnd('/')}/api/webhookwaha/waha";
        }

        #endregion
    }
}
